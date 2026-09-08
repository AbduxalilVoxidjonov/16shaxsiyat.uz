using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.PublicUsers.DeleteAccount;

/// <summary>
/// "Ma'lumotimni o'chiring" (self-service, `docs/08` 5-bo'lim; sabab so'rash — egasining
/// 2026-09-08 qarori). To'rt qadam:
/// 1. `PublicUser.MarkDeleted` — Telegram ID va profil maydonlari tozalanadi (idempotent),
///    sabab/izoh esa SAQLANADI (superadmin ro'yxatida ko'rinishi uchun);
/// 2. BARCHA faol refresh tokenlar bekor qilinadi — mavjud qurilmalar darhol chiqariladi
///    (access token 30 daqiqagacha "tirik" qoladi, lekin `GET /api/me` global filtr tufayli
///    baribir `401` beradi);
/// 3. audit (`PublicUser.Deleted`) — FAQAT sabab KODI (`afterJson`), erkin matnli izoh
///    audit logga YOZILMAYDI (u foydalanuvchi matni, `audit_logs`da kerak emas).
///
/// **Qattiq o'chirish (hard delete) qilinmaydi** — sabab domen qatlamida yozilgan
/// (`PublicUser.MarkDeleted` izohi: `students.public_user_id` FK'si va 5 yillik natija
/// arxivi). Idempotent: takroriy chaqiruv `204` qaytaradi va sabab QAYTA YOZILMAYDI
/// (`request.Reason`/`Comment` shu holatda e'tiborga olinmaydi — `user is null`).
///
/// ⚠️ **Ochiq savol (egasiga):** ommaviy makonda yaratilgan `Student` yozuvidagi F.I.Sh./
/// telefon SHU BOSQICHDA anonimlashtirilmaydi — vazifa shartida faqat `MarkDeleted` va
/// tokenlarni bekor qilish so'ralgan, `Student` anonimlashtirish esa `NormalizedName` bo'yicha
/// takrorlanishni aniqlash (BR-1) mantig'iga ta'sir qiladi. Alohida qaror talab qiladi.
/// </summary>
internal sealed class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DeleteMyAccountCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var user = await _executor.FirstOrDefaultAsync(
            _context.PublicUsers.Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            // Allaqachon o'chirilgan (global filtr yashiradi) — idempotent muvaffaqiyat.
            return Result.Success();
        }

        // `request.Reason` bu yerga `null` bo'lib yeta olmaydi — validator (`NotNull`)
        // handlerdan OLDIN ishlaydi (`ValidationBehavior`).
        user.MarkDeleted(now, request.Reason!.Value, request.Comment);

        var activeTokens = await _executor.ToListAsync(
            _context.PublicRefreshTokens.Where(t => t.PublicUserId == user.Id && t.RevokedAt == null),
            cancellationToken).ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        _context.Add(AuditLog.Create(
            PublicAuditActions.AccountDeleted,
            now,
            entityType: PublicAuditActions.PublicUserEntityType,
            entityId: user.Id,
            afterJson: AuditSnapshot.Serialize(new { Reason = request.Reason.Value.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress)));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

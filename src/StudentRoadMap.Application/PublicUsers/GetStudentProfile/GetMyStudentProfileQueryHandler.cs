using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.StartPublicSession;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.GetStudentProfile;

/// <summary>
/// Saqlangan anketani qaytaradi. `Student` `PublicUserId` bo'yicha qidiriladi — bitta akkaunt
/// → bitta profil (`ux_students_public_user`), shu sabab makon (`SchoolId`) bo'yicha
/// qo'shimcha filtr shart emas. Read-only (`AsNoTracking`).
///
/// `ConsentCurrent` — `PublicConsent.CurrentVersion` bilan solishtiriladi (versiya
/// `StartPublicSessionCommandHandler` bilan BIR manbadan; ikkita nusxa bo'lsa "anketa rozilik
/// so'ramadi, server esa talab qildi" nomuvofiqligi chiqardi).
/// </summary>
internal sealed class GetMyStudentProfileQueryHandler : IRequestHandler<GetMyStudentProfileQuery, Result<MyStudentProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public GetMyStudentProfileQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<MyStudentProfileDto>> Handle(GetMyStudentProfileQuery request, CancellationToken cancellationToken)
    {
        // Global filtr (`DeletedAt == null`) o'chirilgan akkauntni yashiradi — `GetMyProfileQueryHandler` bilan bir xil.
        var user = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.PublicUsers).Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<MyStudentProfileDto>(new Error(ProblemCodes.PublicUserDeleted, "Bu akkaunt mavjud emas."));
        }

        var student = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.PublicUserId == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        var suggestedFullName = SuggestFullName(user);

        if (student is null)
        {
            return Result.Success(new MyStudentProfileDto(
                HasProfile: false,
                FullName: null,
                BirthDate: null,
                Gender: null,
                Phone: null,
                Grade: null,
                Email: null,
                ConsentVersion: null,
                ConsentCurrent: false,
                ParentalConsent: false,
                IsMinor: false,
                SuggestedFullName: suggestedFullName));
        }

        var today = DateOnly.FromDateTime(_dateTime.UtcNow.UtcDateTime);
        var isMinor = student.BirthDate <= today
            && Student.CalculateAge(student.BirthDate, today) < PublicConsent.ParentalConsentRequiredBelowAge;

        return Result.Success(new MyStudentProfileDto(
            HasProfile: true,
            FullName: student.FullName,
            BirthDate: student.BirthDate,
            Gender: student.Gender,
            Phone: student.Phone.Value,
            Grade: student.Grade == Student.NoGrade ? null : student.Grade,
            Email: student.Email,
            ConsentVersion: student.ConsentVersion,
            ConsentCurrent: student.ConsentVersion == PublicConsent.CurrentVersion,
            ParentalConsent: student.ParentalConsent,
            IsMinor: isMinor,
            SuggestedFullName: suggestedFullName));
    }

    /// <summary>
    /// Telegram ismidan F.I.Sh. TAKLIFI: `Familiya Ism` tartibida (o'zbek rasmiy yozuvi);
    /// biri yo'q bo'lsa bori; ikkalasi yo'q — `null`. Bu qiymat AI'ga hech qachon
    /// tushmaydi (`CLAUDE.md` 5-qoida) — faqat anketa maydonini oldindan to'ldirish uchun.
    /// </summary>
    private static string? SuggestFullName(PublicUser user)
    {
        var parts = new[] { user.LastName, user.FirstName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        var joined = string.Join(' ', parts);
        return joined.Length == 0 ? null : joined;
    }
}

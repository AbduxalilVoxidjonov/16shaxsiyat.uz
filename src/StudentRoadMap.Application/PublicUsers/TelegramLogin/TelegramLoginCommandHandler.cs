using System.Globalization;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Application.PublicUsers.TelegramLogin;

/// <summary>
/// Telegram Login Widget orqali kirish/ro'yxatdan o'tish (`docs/08` 2a-bo'lim).
///
/// Oqim:
/// 1. Bot tokeni sozlanganmi (`503` — server muammosi, mijozni chalg'itmaslik uchun);
/// 2. `auth_date` yangiligi (<see cref="MaxAuthDateAge"/>);
/// 3. imzo (`ITelegramLoginVerifier`, doimiy vaqtda solishtirish);
/// 4. `TelegramId` bo'yicha akkaunt topiladi yoki yaratiladi;
/// 5. access token (`aud = Jwt:PublicAudience`) + refresh token (rotatsiyalanadigan).
///
/// **Nima uchun `auth_date` imzodan OLDIN tekshiriladi:** ikkalasi ham `401` beradi, lekin
/// eskirgan ma'lumotni imzo hisoblashiga olib bormaslik ARZONROQ va `TELEGRAM_AUTH_EXPIRED`
/// mijozga aniqroq harakat ("tugmani qayta bosing") beradi. Bu ma'lumot sizib chiqish emas:
/// `auth_date` imzoning bir qismi, ya'ni uni o'zgartirish imzoni ham buzadi.
/// </summary>
internal sealed class TelegramLoginCommandHandler : IRequestHandler<TelegramLoginCommand, Result<TelegramLoginResult>>
{
    /// <summary>
    /// `auth_date` shu muddatdan eski bo'lsa rad etiladi. **24 soat** — Telegram rasmiy
    /// namunasida ko'rsatilgan (`if (auth_date) < (now - 86400)`) qiymat: widget ma'lumoti
    /// bir marta ishlatiladigan bo'lsa ham, foydalanuvchi sahifani ochib qo'yib keyinroq
    /// tugmani bosishi mumkin, shu sabab bir kunlik oyna amaliy jihatdan yetarli va
    /// qayta-ishlatish (replay) oynasini cheklaydi.
    /// </summary>
    private static readonly TimeSpan MaxAuthDateAge = TimeSpan.FromHours(24);

    /// <summary>
    /// Kelajakdagi `auth_date` uchun ruxsat etilgan soat farqi — Telegram serveri va bizning
    /// serverimiz soatlari biroz farq qilishi mumkin (JWT `ClockSkew` bilan bir xil asos).
    /// </summary>
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private const int RefreshTokenByteLength = 64;
    private const int MaxUserAgentLength = 300;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ITelegramLoginVerifier _verifier;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public TelegramLoginCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        ITelegramLoginVerifier verifier,
        IJwtTokenService jwtTokenService,
        ITokenGenerator tokenGenerator,
        IIpHasher ipHasher,
        IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _verifier = verifier;
        _jwtTokenService = jwtTokenService;
        _tokenGenerator = tokenGenerator;
        _ipHasher = ipHasher;
        _appSettings = appSettings;
    }

    public async Task<Result<TelegramLoginResult>> Handle(TelegramLoginCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var ipHash = _ipHasher.Hash(request.IpAddress);
        var userAgent = Truncate(request.UserAgent);

        if (!_verifier.IsConfigured)
        {
            return Result.Failure<TelegramLoginResult>(new Error(
                ProblemCodes.TelegramAuthNotConfigured,
                "Telegram orqali kirish hozircha sozlanmagan."));
        }

        var authDate = DateTimeOffset.FromUnixTimeSeconds(request.AuthDate);
        if (authDate < now - MaxAuthDateAge || authDate > now + MaxClockSkew)
        {
            await WriteFailedAuditAsync(now, ipHash, userAgent, "auth_date", cancellationToken).ConfigureAwait(false);

            return Result.Failure<TelegramLoginResult>(new Error(
                ProblemCodes.TelegramAuthExpired,
                "Telegram ma'lumoti eskirgan. Iltimos, qaytadan kiring."));
        }

        if (!_verifier.Verify(BuildDataFields(request), request.Hash))
        {
            await WriteFailedAuditAsync(now, ipHash, userAgent, "hash", cancellationToken).ConfigureAwait(false);

            return Result.Failure<TelegramLoginResult>(new Error(
                ProblemCodes.TelegramAuthInvalid,
                "Telegram ma'lumotini tasdiqlab bo'lmadi."));
        }

        // Global filtr o'chirilgan (anonimlashtirilgan) akkauntlarni YASHIRADI
        // (`AppDbContext.OnModelCreating`) — ya'ni akkauntini o'chirgan foydalanuvchi
        // qaytadan kirsa YANGI akkaunt oladi (`PublicUser.MarkDeleted` izohi: bu AYNAN
        // kutilgan xatti-harakat, chunki `telegram_id` allaqachon tozalangan).
        var user = await _executor.FirstOrDefaultAsync(
            _context.PublicUsers.Where(u => u.TelegramId == request.Id),
            cancellationToken).ConfigureAwait(false);

        var isNewUser = user is null;

        if (user is null)
        {
            user = PublicUser.Create(
                Guid.NewGuid(),
                request.Id,
                now,
                username: request.Username,
                firstName: request.FirstName,
                lastName: request.LastName,
                photoUrl: request.PhotoUrl);

            _context.Add(user);
        }
        else
        {
            user.RecordLogin(now, request.Username, request.FirstName, request.LastName, request.PhotoUrl);
        }

        var accessToken = _jwtTokenService.CreatePublicUserAccessToken(user.Id, now);

        var rawRefreshToken = _tokenGenerator.GenerateUrlSafeToken(RefreshTokenByteLength);
        var refreshExpiresAt = now.AddDays(_appSettings.RefreshTokenDays);

        _context.Add(PublicRefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            TokenHash.Compute(rawRefreshToken),
            refreshExpiresAt,
            now,
            ipHash));

        _context.Add(AuditLog.Create(
            PublicAuditActions.LoginSucceeded,
            now,
            entityType: PublicAuditActions.PublicUserEntityType,
            entityId: user.Id,
            ipHash: ipHash,
            userAgent: userAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new TelegramLoginResult(accessToken.Token, accessToken.ExpiresInSeconds, ToDto(user))
        {
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            IsNewUser = isNewUser,
        };

        return Result.Success(result);
    }

    /// <summary>
    /// `data_check_string` uchun maydonlar to'plami — `hash` KIRMAYDI (Telegram algoritmi).
    /// Bo'sh/`null` qiymatlar TUSHIRIB QOLDIRILADI: Telegram mavjud bo'lmagan maydonni
    /// (masalan `last_name`) umuman YUBORMAYDI, ya'ni imzo ham usiz hisoblangan — bo'sh satr
    /// bilan qo'shib qo'yish imzoni buzardi.
    /// </summary>
    private static Dictionary<string, string> BuildDataFields(TelegramLoginCommand request)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = request.Id.ToString(CultureInfo.InvariantCulture),
            ["auth_date"] = request.AuthDate.ToString(CultureInfo.InvariantCulture),
        };

        AddIfPresent(fields, "first_name", request.FirstName);
        AddIfPresent(fields, "last_name", request.LastName);
        AddIfPresent(fields, "username", request.Username);
        AddIfPresent(fields, "photo_url", request.PhotoUrl);

        return fields;
    }

    private static void AddIfPresent(IDictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            fields[key] = value;
        }
    }

    /// <summary>
    /// Rad etilgan urinish auditi. `EntityId` ATAYLAB yozilmaydi — imzo tasdiqlanmagan
    /// `id` ga ishonib bo'lmaydi (istalgan raqam yuborilishi mumkin), aks holda hujumchi
    /// begona foydalanuvchi nomiga "xato kirish" yozuvlarini to'ldira olardi.
    /// </summary>
    private async Task WriteFailedAuditAsync(DateTimeOffset now, string? ipHash, string? userAgent, string reason, CancellationToken cancellationToken)
    {
        _context.Add(AuditLog.Create(
            PublicAuditActions.LoginFailed,
            now,
            ipHash: ipHash,
            userAgent: userAgent,
            afterJson: $"{{\"reason\":\"{reason}\"}}"));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static PublicUserDto ToDto(PublicUser user) =>
        new(user.Id, user.Username, user.FirstName, user.LastName, user.PhotoUrl, user.CreatedAt, user.LastLoginAt);

    private static string? Truncate(string? value) =>
        string.IsNullOrEmpty(value) || value.Length <= MaxUserAgentLength ? value : value[..MaxUserAgentLength];
}

namespace StudentRoadMap.Application.Common.Models;

/// <summary>
/// Xato `code` konstantalari va ularning HTTP status kodiga xaritasi — `docs/06-arxitektura.md`
/// 6-bo'limidagi jadvalga aynan mos. `Api/Middleware/ExceptionHandlingMiddleware` va
/// `Result` xatolarini `ProblemDetails`ga aylantiruvchi kontroller yordamchisi shundan foydalanadi.
/// </summary>
public static class ProblemCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string SchoolInactive = "SCHOOL_INACTIVE";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string DuplicateAssessment = "DUPLICATE_ASSESSMENT";
    public const string TestNotUnlocked = "TEST_NOT_UNLOCKED";
    public const string SystemTestLocked = "SYSTEM_TEST_LOCKED";
    public const string TestNotPublishable = "TEST_NOT_PUBLISHABLE";
    public const string TestInUse = "TEST_IN_USE";
    public const string RateLimited = "RATE_LIMITED";
    public const string AiProviderError = "AI_PROVIDER_ERROR";
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>
    /// `docs/06` jadvalida yo'q, lekin ommaviy oqim uchun zarur bo'lgan qo'shimcha kod:
    /// maktab kirish kodi (`AccessCode`) noto'g'ri/berilmagan. Jadvalda alohida qatori yo'q —
    /// `400`ga xaritalanadi (umumiy validatsiya xatosi bilan bir xil darajada). PM'ga savol:
    /// `docs/07`ga rasman qo'shilishi kerakmi?
    /// </summary>
    public const string AccessCodeInvalid = "ACCESS_CODE_INVALID";

    /// <summary>`Error.Code` → HTTP status. Ro'yxatda yo'q kod uchun standart qiymat `409` (domen holat mashinasi konflikti).</summary>
    public static readonly IReadOnlyDictionary<string, int> HttpStatusByCode = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [ValidationError] = StatusCodes.Status400BadRequest,
        [Unauthorized] = StatusCodes.Status401Unauthorized,
        [Forbidden] = StatusCodes.Status403Forbidden,
        [NotFound] = StatusCodes.Status404NotFound,
        [SchoolInactive] = StatusCodes.Status410Gone,
        [SessionExpired] = StatusCodes.Status410Gone,
        [DuplicateAssessment] = StatusCodes.Status409Conflict,
        [TestNotUnlocked] = StatusCodes.Status409Conflict,
        [SystemTestLocked] = StatusCodes.Status409Conflict,
        [TestNotPublishable] = StatusCodes.Status400BadRequest,
        [TestInUse] = StatusCodes.Status409Conflict,
        [RateLimited] = StatusCodes.Status429TooManyRequests,
        [AiProviderError] = StatusCodes.Status502BadGateway,
        [InternalError] = StatusCodes.Status500InternalServerError,
        [AccessCodeInvalid] = StatusCodes.Status400BadRequest,
    };

    /// <summary>Default (`docs/06` jadvaliga kirmagan domen kodlari uchun) — holat mashinasi konflikti.</summary>
    public const int DefaultDomainErrorStatus = StatusCodes.Status409Conflict;

    /// <summary>
    /// `Microsoft.AspNetCore.Http.StatusCodes` ni takrorlamaslik uchun mini-nusxa — `Application`
    /// qatlamida ASP.NET Core paketiga bog'lanmaslik kerak (`docs/06` 3-bo'lim), shu sabab HTTP
    /// status kodlari shu yerda o'zgarmas butun son sifatida e'lon qilingan.
    /// </summary>
    private static class StatusCodes
    {
        public const int Status400BadRequest = 400;
        public const int Status401Unauthorized = 401;
        public const int Status403Forbidden = 403;
        public const int Status404NotFound = 404;
        public const int Status409Conflict = 409;
        public const int Status410Gone = 410;
        public const int Status429TooManyRequests = 429;
        public const int Status500InternalServerError = 500;
        public const int Status502BadGateway = 502;
    }
}

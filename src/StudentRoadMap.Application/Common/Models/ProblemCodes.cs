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

    /// <summary>`prompts/34` — `AssessmentProgram.Publish` kamida bitta test talab qiladi (`TEST_NOT_PUBLISHABLE` bilan bir xil uslub, 400).</summary>
    public const string ProgramNotPublishable = "PROGRAM_NOT_PUBLISHABLE";
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

    /// <summary>
    /// `prompts/34` C9-band: maktabda bir nechta dastur mavjud, lekin `programCode` berilmagan
    /// — o'quvchi qaysi dasturni tanlashi kerakligini bildirmagan. `docs/06`da yo'q, PM'ga
    /// savol: rasman kiritilsinmi (`AccessCodeInvalid` bilan bir xil uslub, 400).
    /// </summary>
    public const string ProgramRequired = "PROGRAM_REQUIRED";

    /// <summary>
    /// `docs/07` §1.1 (2026-09-03): havola VA token TO'G'RI, maktab FAOL — lekin bu maktab uchun
    /// bironta mavjud dastur yo'q (`ProgramAvailability`). Bu `NOT_FOUND` dan ATAYIN ajratilgan:
    /// "havola noto'g'ri" (o'quvchi maktabga havolani qayta so'rab murojaat qiladi) va "havola
    /// to'g'ri, lekin test hali tayyorlanmagan" (maktab admini dasturni yoqishi kerak) — butunlay
    /// boshqa harakat talab qiladi. `409` ga xaritalanadi: resurs BOR, lekin joriy holati
    /// so'rovni bajarishga imkon bermaydi (`404`/`410` allaqachon boshqa ma'noda band —
    /// `NOT_FOUND` noma'lum slug/noto'g'ri token, `SCHOOL_INACTIVE` o'chirilgan maktab).
    /// PM'ga savol: `docs/06` 6-bo'lim jadvaliga rasman kiritilsinmi?
    /// </summary>
    public const string NoProgramAvailable = "NO_PROGRAM_AVAILABLE";

    /// <summary>
    /// P13 (`prompts/13-auth-va-jwt.md`) superadmin auth oqimi uchun qo'shildi — PM tomonidan
    /// `docs/06`ga rasman kiritilgan (423).
    /// </summary>
    public const string AccountLocked = "ACCOUNT_LOCKED";

    /// <summary>TOTP yoqilgan hisobda `totpCode` berilmagan — ikkinchi bosqich talab qilinadi. `docs/06`da (401).</summary>
    public const string TotpRequired = "TOTP_REQUIRED";

    /// <summary>TOTP allaqachon yoqilgan (`EnableTotp` ikkinchi marta chaqirilganda). `docs/06`da (409).</summary>
    public const string TotpAlreadyEnabled = "TOTP_ALREADY_ENABLED";

    /// <summary>TOTP yoqilmagan hisobda `DisableTotp` chaqirilganda. `docs/06`da (409).</summary>
    public const string TotpNotEnabled = "TOTP_NOT_ENABLED";

    // --- P46 (2FA tasdiqlash bosqichi) — `POST /api/auth/totp/confirm`. `docs/06` 6-bo'lim
    // jadvaliga qo'shildi. ---

    /// <summary>
    /// `confirm` chaqirildi, lekin kutish holatidagi sir yo'q — `enable` umuman chaqirilmagan
    /// (yoki `DisableTotp`/yangi `enable` uni tozalagan). Mijoz jarayonni boshidan boshlashi
    /// kerak (409).
    /// </summary>
    public const string TotpEnrollmentNotStarted = "TOTP_ENROLLMENT_NOT_STARTED";

    /// <summary>
    /// Kutish holatidagi sir bor, lekin `AdminUser.PendingTotpEnrollmentLifetime` (10 daqiqa)
    /// muddati o'tgan — QR qayta so'ralishi kerak (409).
    /// </summary>
    public const string TotpEnrollmentExpired = "TOTP_ENROLLMENT_EXPIRED";

    /// <summary>
    /// `confirm` ga yuborilgan 6 xonali kod kutish holatidagi sirga mos kelmadi (400). Login
    /// oqimidagi `UNAUTHORIZED` dan farqli — bu yerda foydalanuvchi ALLAQACHON autentifikatsiyadan
    /// o'tgan, faqat ilova soati/skaner xato bo'lishi mumkin.
    /// </summary>
    public const string TotpCodeInvalid = "TOTP_CODE_INVALID";

    /// <summary>
    /// Optimistik konkurentlik ziddiyati (`ConcurrencyConflictException`) — ikki bir vaqtdagi
    /// so'rov bir xil yozuvni o'zgartirmoqchi bo'lganda (masalan, bitta TOTP kodi bilan ikki
    /// parallel login urinishi, QA topilmasi). Mijoz qaytadan urinib ko'rishi kerak.
    /// </summary>
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    /// <summary>
    /// `P14` (`prompts/14-admin-maktab-va-oquvchi-api.md`) MAXSUS DIQQAT #3: maktab slug'i
    /// uchun `ux_schools_slug` unique cheklovi ChIN bir vaqtdagi poyga holatida buzilganda
    /// (`UniqueConstraintViolationException`) — jimgina `500` o'rniga tushunarli `409`.
    /// `docs/06`da yo'q, PM'ga savol: rasman kiritilsinmi?
    /// </summary>
    public const string UniqueConstraintConflict = "UNIQUE_CONSTRAINT_CONFLICT";

    /// <summary>
    /// `P14` MAXSUS DIQQAT #5: maktabni o'chirishga (soft) urinishda unda hali o'quvchi(lar)
    /// bo'lsa. `docs/06`da yo'q — `TestInUse` (409, testni o'chirishda ishlatilgan bo'lsa) bilan
    /// bir xil uslubda, PM'ga savol: rasman kiritilsinmi?
    /// </summary>
    public const string SchoolHasStudents = "SCHOOL_HAS_STUDENTS";

    // --- P37 (`prompts/37-katalog-crud-backend.md`) — test katalogi CRUD (`docs/07` §3.4).
    // Quyidagi to'rttasi allaqachon `DefaultDomainErrorStatus` (409) ga tushardi — bu yerda
    // faqat hujjatlashtirish uchun aniq ro'yxatga qo'shilgan, xatti-harakat o'zgarmadi. ---

    /// <summary>Bir xil `Code` bilan ikkinchi shkala qo'shishga urinish (`TestDefinition.AddScale`).</summary>
    public const string ScaleCodeDuplicate = "SCALE_CODE_DUPLICATE";

    /// <summary>Shkalada savollar bo'lganda o'chirishga urinish (`docs/07` §3.4: "DELETE .../scales/{scaleId} — savollari bo'lsa 409").</summary>
    public const string ScaleInUse = "SCALE_IN_USE";

    /// <summary>Bitta anketa ichida bir xil `Code` bilan ikkinchi savol qo'shish/import (`TestDefinition.AddQuestion`).</summary>
    public const string QuestionCodeDuplicate = "QUESTION_CODE_DUPLICATE";

    /// <summary>Holat mashinasi noto'g'ri o'tish (masalan `Archived` dan `Published`ga) — `TestDefinition.Publish`/`Archive`.</summary>
    public const string TestDefinitionInvalidTransition = "TEST_DEFINITION_INVALID_TRANSITION";

    // --- P39 (Excel shablon va yuklash, `docs/07` §3.4) ---

    /// <summary>
    /// `POST /api/admin/catalog/import/parse-excel` — yuklangan fayl umuman o'qilmadi: ZIP
    /// (Open XML) emas, buzilgan, eski `.xls` yoki makrolı `.xlsm`, yoki qator chegarasidan
    /// oshgan. `400` — mijoz boshqa fayl yuborishi kerak. Fayl HAJMI chegarasi bu kodga
    /// TUSHMAYDI: u `413 PAYLOAD_TOO_LARGE` (`ProblemDetailsSetup`, P31).
    /// </summary>
    public const string ImportFileInvalid = "IMPORT_FILE_INVALID";

    // --- P47 (ommaviy makon va Telegram kirishi) — `docs/06` 6-bo'lim jadvaliga qo'shildi. ---

    /// <summary>
    /// `POST /api/auth/telegram` — Telegram imzosi (`hash`) mos kelmadi. Mijoz uchun bu
    /// "qaytadan kiring" degani (401). Sabab ATAYIN oshkor qilinmaydi (soxta imzo, o'zgartirilgan
    /// maydon yoki boshqa botning tokeni — hammasi bir xil javob).
    /// </summary>
    public const string TelegramAuthInvalid = "TELEGRAM_AUTH_INVALID";

    /// <summary>
    /// `auth_date` <see cref="TelegramAuthInvalid"/> dan ATAYIN ajratilgan: imzo TO'G'RI, lekin
    /// ma'lumot eskirgan (24 soatdan oshgan) — mijoz Telegram tugmasini QAYTA bosishi kifoya,
    /// hech narsa buzilmagan (401).
    /// </summary>
    public const string TelegramAuthExpired = "TELEGRAM_AUTH_EXPIRED";

    /// <summary>
    /// `Telegram:BotToken` berilmagan — bu server SOZLAMASI muammosi, mijozning aybi emas.
    /// `401` qaytarish chalg'ituvchi bo'lardi ("kirishim noto'g'ri" deb o'ylardi), shu sabab
    /// `503` (xizmat vaqtincha mavjud emas).
    /// </summary>
    public const string TelegramAuthNotConfigured = "TELEGRAM_AUTH_NOT_CONFIGURED";

    /// <summary>
    /// Ommaviy makon (`SchoolKind.PublicSpace`) bazada topilmadi — seed bajarilmagan
    /// (`docker compose --profile init run --rm seed`). Server holati muammosi (409).
    /// </summary>
    public const string PublicSpaceNotConfigured = "PUBLIC_SPACE_NOT_CONFIGURED";

    /// <summary>
    /// `PublicUser.MarkDeleted` chaqirilgan akkaunt bilan amal bajarishga urinish
    /// (`DomainException("PUBLIC_USER_DELETED")`). Token hali amal qilayotgan bo'lishi mumkin
    /// (access token 30 daqiqa) — mijoz uchun bu "sessiya tugadi" (401).
    /// </summary>
    public const string PublicUserDeleted = "PUBLIC_USER_DELETED";

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
        [ProgramNotPublishable] = StatusCodes.Status400BadRequest,
        [TestInUse] = StatusCodes.Status409Conflict,
        [RateLimited] = StatusCodes.Status429TooManyRequests,
        [AiProviderError] = StatusCodes.Status502BadGateway,
        [InternalError] = StatusCodes.Status500InternalServerError,
        [AccessCodeInvalid] = StatusCodes.Status400BadRequest,
        [ProgramRequired] = StatusCodes.Status400BadRequest,
        [NoProgramAvailable] = StatusCodes.Status409Conflict,
        [AccountLocked] = StatusCodes.Status423Locked,
        [TotpRequired] = StatusCodes.Status401Unauthorized,
        [TotpAlreadyEnabled] = StatusCodes.Status409Conflict,
        [TotpNotEnabled] = StatusCodes.Status409Conflict,
        [TotpEnrollmentNotStarted] = StatusCodes.Status409Conflict,
        [TotpEnrollmentExpired] = StatusCodes.Status409Conflict,
        [TotpCodeInvalid] = StatusCodes.Status400BadRequest,
        [ConcurrencyConflict] = StatusCodes.Status409Conflict,
        [UniqueConstraintConflict] = StatusCodes.Status409Conflict,
        [SchoolHasStudents] = StatusCodes.Status409Conflict,
        [ScaleCodeDuplicate] = StatusCodes.Status409Conflict,
        [ScaleInUse] = StatusCodes.Status409Conflict,
        [QuestionCodeDuplicate] = StatusCodes.Status409Conflict,
        [TestDefinitionInvalidTransition] = StatusCodes.Status409Conflict,
        [ImportFileInvalid] = StatusCodes.Status400BadRequest,
        [TelegramAuthInvalid] = StatusCodes.Status401Unauthorized,
        [TelegramAuthExpired] = StatusCodes.Status401Unauthorized,
        [TelegramAuthNotConfigured] = StatusCodes.Status503ServiceUnavailable,
        [PublicSpaceNotConfigured] = StatusCodes.Status409Conflict,
        [PublicUserDeleted] = StatusCodes.Status401Unauthorized,
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
        public const int Status423Locked = 423;
        public const int Status429TooManyRequests = 429;
        public const int Status500InternalServerError = 500;
        public const int Status502BadGateway = 502;
        public const int Status503ServiceUnavailable = 503;
    }
}

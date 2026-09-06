namespace StudentRoadMap.Application.Admin.PublicSpace;

/// <summary>
/// Ommaviy makon havolasi ISHLAYDIMI — mezon maktablarnikidan AJRALGAN EMAS, aksincha
/// AYNAN BIR XIL (`SchoolLinkHealthEvaluator`, `ProgramAvailability`): "hech kim test
/// boshlay olmaydi" sharti ikki oqimda ham bitta joydan hisoblanadi. Ajratilgan narsa —
/// faqat DTO nomi va UI matni ("maktab" so'zi bu bo'limda ishlatilmaydi).
///
/// `Status` qiymatlari: `Ok`, `NoProgramsAtAll`, `NoProgramAssigned`, `ProgramsDeactivated`,
/// `ProgramsWithoutTests` (`SchoolLinkHealthStatus`).
///
/// `AvailableProgramCount == 0` ⟺ ommaviy foydalanuvchi test boshlashga urinsa
/// `409 NO_PROGRAM_AVAILABLE` oladi. `UsableProgramCount == 0` bo'lsa dastur bor, lekin
/// ichida yaroqli (nashr qilingan, faol, savoli bor) test yo'q — sessiya bo'sh chiqadi.
/// </summary>
public sealed record AdminPublicSpaceAvailabilityDto(
    string Status,
    int AvailableProgramCount,
    int UsableProgramCount);

/// <summary>
/// Ommaviy makonga BIRIKTIRILGAN bitta dastur (`school_programs` yozuvi).
///
/// `IsActive == false` yoki `Status != "Published"` — dastur mavjud, lekin ISHLAMAYDI;
/// UI shuni aniq ogohlantirish sifatida ko'rsatadi (2026-09-03 jonli hodisasi: yagona dastur
/// o'chirilgan edi, panelda esa hech qanday belgi yo'q edi). `HasUsableTest == false` —
/// dastur ichida nashr qilingan/faol va savoli bor test yo'q.
///
/// Dasturni yoqish/o'chirish bu yerda EMAS — u `Admin/Programs` bo'limining ishi
/// (`POST /api/admin/programs/{id}/toggle-active`); bu bo'lim faqat BIRIKTIRISHNI boshqaradi.
/// </summary>
public sealed record AdminPublicSpaceProgramDto(
    Guid Id,
    string Code,
    string NameUz,
    string Status,
    string Visibility,
    bool IsActive,
    int TestCount,
    bool HasUsableTest);

/// <summary>
/// Ommaviy makon statistikasi. Atamalar ATAYLAB maktabnikidan farq qiladi
/// (`AdminSchoolStatsDto` da "o'quvchi") — bu yerda ular MAKTABSIZ tashqi FOYDALANUVCHILAR.
///
/// - `UserCount` — ommaviy makonda ro'yxatdan o'tgan foydalanuvchilar (soft-delete
///   qilinganlar hisobga olinmaydi — `Student` global query filtri).
/// - `TotalAssessments` — shu makondagi barcha sessiyalar (`Draft` ham).
/// - `InProgressCount` — hozir jarayonda (`Status == InProgress`).
/// - `CompletedCount` — yakunlangan (`CompletedAt != null`).
/// - `AnalyzedCount` — AI tahlili tugagan (`Status == Analyzed`).
/// - `LastActivityAt` — eng so'nggi sessiya harakati; sessiya bo'lmasa `null` (`0` EMAS —
///   `docs/06` §8: "ma'lumot yo'q ≠ nol").
/// </summary>
public sealed record AdminPublicSpaceStatsDto(
    int UserCount,
    int TotalAssessments,
    int InProgressCount,
    int CompletedCount,
    int AnalyzedCount,
    DateTimeOffset? LastActivityAt);

/// <summary>
/// `GET /api/admin/public-space` — ommaviy makonning to'liq holati (2026-09-06).
///
/// <para>
/// **Nima uchun `AdminSchoolDetailDto` qayta ishlatilmadi:** u maktab shartnomasi
/// (`region`/`district`/`schoolNumber`/`contactPerson`/`accessCode`/QR kod) — ommaviy makonda
/// bularning hech biri ma'noga ega emas (`School.CreatePublicSpace` ularga barqaror "Ommaviy"
/// yoki `null` yozadi). Bir DTO'ni ikki ma'noda ishlatish panelda "Viloyat: Ommaviy" kabi
/// bema'ni qatorlarga olib kelardi.
/// </para>
/// <para>
/// **`PublicUrl`** — maktabnikidan BOSHQA shakl: ommaviy oqimda `slug`+`accessToken` havolasi
/// EMAS, kabinetga kirish sahifasi (`{PublicWebBaseUrl}/kirish`) ishlatiladi — tashqi
/// foydalanuvchi Telegram orqali autentifikatsiyadan o'tadi, maxfiy havola tarqatilmaydi.
/// Shu sabab bu bo'limda "havolani qayta generatsiya qilish" ham YO'Q.
/// </para>
/// <para>
/// **`IsActive` faqat O'QISH uchun:** ommaviy makonni faolsizlantirish/o'chirish domen
/// darajasida taqiqlangan (`School.Deactivate`/`MarkDeleted` →
/// `SCHOOL_PUBLIC_SPACE_PROTECTED`), shu sabab mos endpoint UMUMAN QO'SHILMAGAN.
/// </para>
/// </summary>
public sealed record AdminPublicSpaceDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    bool ShowResultToStudent,
    int DailyRegistrationLimit,
    string PublicUrl,
    AdminPublicSpaceAvailabilityDto Availability,
    IReadOnlyList<AdminPublicSpaceProgramDto> Programs,
    AdminPublicSpaceStatsDto Stats);

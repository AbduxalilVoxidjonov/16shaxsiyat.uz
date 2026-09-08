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
/// `State != "Active"` — dastur mavjud, lekin ISHLAMAYDI; UI shuni aniq ogohlantirish
/// sifatida ko'rsatadi (2026-09-03 jonli hodisasi: yagona dastur o'chirilgan edi, panelda
/// esa hech qanday belgi yo'q edi). `HasUsableTest == false` — dastur ichida nashr
/// qilingan/faol va savoli bor test yo'q.
///
/// **2026-09-06:** `status` + `isActive` juftligi o'rniga bitta `state` beriladi —
/// `AdminProgramListItemDto` bilan AYNAN bir xil hosila holat va bir xil manba
/// (`AssessmentProgram.State`). Ilgari bu yerda ham ikkita belgi bor edi va ular
/// "Arxiv + Faol" kabi ziddiyatni ko'rsatishi mumkin edi.
///
/// Dasturni yoqish/o'chirish bu yerda EMAS — u `Admin/Programs` bo'limining ishi
/// (`POST /api/admin/programs/{id}/toggle-active`); bu bo'lim faqat BIRIKTIRISHNI boshqaradi.
/// </summary>
public sealed record AdminPublicSpaceProgramDto(
    Guid Id,
    string Code,
    string NameUz,
    string State,
    string Visibility,
    int TestCount,
    bool HasUsableTest);

/// <summary>
/// Ommaviy makon statistikasi. Atamalar ATAYLAB maktabnikidan farq qiladi
/// (`AdminSchoolStatsDto` da "o'quvchi") — bu yerda ular MAKTABSIZ tashqi FOYDALANUVCHILAR.
///
/// - `UserCount` — ro'yxatdan o'tgan (Telegram orqali kirgan) FAOL akkauntlar —
///   `public_users`, `Student` EMAS (2026-09-07: ilgari makondagi `Student`lar sanalardi, bu
///   hali anketa to'ldirmaganlarni tashlab ketardi va foydalanuvchilar ro'yxatining
///   `totalCount`i bilan mos kelmasdi). O'chirilganlar KIRMAYDI (`PublicUsers` global filtri).
/// - `DeletedUserCount` — "ma'lumotimni o'chiring" qilgan (anonimlashtirilgan) akkauntlar;
///   ular ro'yxatda ko'rinmaydi, faqat shu son bilan hisobga olinadi.
/// - `TotalAssessments` — shu makondagi barcha sessiyalar (`Draft` ham).
/// - `InProgressCount` — hozir jarayonda (`Status == InProgress`).
/// - `CompletedCount` — yakunlangan (`CompletedAt != null`).
/// - `AnalyzedCount` — AI tahlili tugagan (`Status == Analyzed`).
/// - `LastActivityAt` — eng so'nggi sessiya harakati; sessiya bo'lmasa `null` (`0` EMAS —
///   `docs/06` §8: "ma'lumot yo'q ≠ nol").
/// </summary>
public sealed record AdminPublicSpaceStatsDto(
    int UserCount,
    int DeletedUserCount,
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

/// <summary>Foydalanuvchining Telegram profili — hammasi ixtiyoriy (Telegram `username`/`last_name`ni har doim bermaydi).</summary>
public sealed record AdminPublicUserTelegramDto(
    string? FirstName,
    string? LastName,
    string? Username);

/// <summary>
/// Foydalanuvchi sessiyalari soni. `Completed` — `CompletedAt != null` (`Completed`/`Analyzing`/
/// `Analyzed`/`AnalysisFailed`), `InProgress` — `Draft`/`InProgress` (hali yakunlanmagan va
/// tashlab ketilmagan). `Total - Completed - InProgress` = `Abandoned`.
/// </summary>
public sealed record AdminPublicUserAssessmentCountsDto(
    int Total,
    int Completed,
    int InProgress);

/// <summary>
/// Yakunlanmagan sessiyada foydalanuvchi QAYERDA to'xtagan — `SessionProgressCalculator`
/// (ommaviy `GET /api/public/sessions/me` bilan BIR XIL qoida) bo'yicha:
/// <list type="bullet">
///   <item>`TestsTotal`/`TestsCompleted` — sessiyadagi bloklar va yakunlanganlari;</item>
///   <item>`CurrentTestNumber` — joriy blokning 1 dan boshlanadigan tartib raqami
///   ("4 dan 2-blok"), `CurrentTestCode`/`CurrentTestName` — o'sha blok; hamma blok yakunlangan
///   (lekin sessiya hali `Complete` qilinmagan) bo'lsa uchalasi `null`;</item>
///   <item>`Answered`/`QuestionsTotal` — JORIY blokdagi javoblar (masalan `17/44`).</item>
/// </list>
/// </summary>
public sealed record AdminPublicUserProgressDto(
    int TestsTotal,
    int TestsCompleted,
    int? CurrentTestNumber,
    string? CurrentTestCode,
    string? CurrentTestName,
    int Answered,
    int QuestionsTotal);

/// <summary>
/// Foydalanuvchining OXIRGI (`StartedAt` bo'yicha) sessiyasi. `Progress` faqat yakunlanmagan
/// sessiyada (`CompletedAt == null` — `Draft`/`InProgress`/`Abandoned`); yakunlanganda `null`.
/// </summary>
public sealed record AdminPublicUserLastAssessmentDto(
    Guid Id,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    AdminPublicUserProgressDto? Progress);

/// <summary>
/// `GET /api/admin/public-space/users` qatori (2026-09-07, `Phone` 2026-09-08 qo'shildi).
/// Ro'yxat MANBAI — `public_users` (Telegram akkaunti), `Student` EMAS: ro'yxatdan o'tgan,
/// lekin hali anketa to'ldirmagan foydalanuvchi ham ko'rinishi kerak (egasining talabi).
/// Shu sabab `StudentId`/`FullName`/`Phone`/`Age`/`Grade` NULLABLE — anketa yo'q bo'lsa
/// `null`. `Grade` — `Student.NoGrade` (0) bo'lsa ham `null` ("sinf yo'q" — kattalar/talabalar).
///
/// <para>
/// **`Phone` manbai — `students.phone` (anketa), `public_users`da EMAS:** Telegram Login
/// Widget telefon raqamini bermaydi, shu sabab `PublicUser`da bunday ustun yo'q va
/// qo'shilmaydi ham. Yagona mavjud raqam — anketani to'ldirganda kiritilgan
/// `Student.Phone` (`+998XXXXXXXXX`, majburiy maydon). Anketa hali to'ldirilmagan bo'lsa
/// `null` — egasining so'rovi bo'yicha Telegram orqali ro'yxatdan o'tgan foydalanuvchilarning
/// raqami ham shu ro'yxatda ko'rinsin.
/// </para>
/// <para>
/// **O'chirilgan (anonimlashtirilgan, `DeletedAt != null`) akkauntlar ENDI RO'YXATGA KIRADI**
/// (2026-09-08, egasining qarori — ilgari `PublicUsers` global filtri ularni yashirar edi):
/// `DeletedAt`/`DeletionReason`/`DeletionComment` orqali "nega o'chirilgani" ko'rinadi.
/// `Telegram.*` bunday qatorda `null` (anonimlashtirilgan), `FullName`/`Phone` esa
/// `Student` yozuvi o'chirilmagani uchun (bu bosqichda anonimlashtirilmaydi — handler izohi)
/// odatda saqlanib qoladi. `?status=deleted` — faqat shular; boshqa filtrlar sessiya holatiga
/// qarab ularni ham qamrab olishi mumkin. Bu ADMIN API — `Id`lar qaytariladi (`CLAUDE.md`
/// 8-qoida faqat ommaviy API uchun).
/// </para>
/// </summary>
public sealed record AdminPublicUserListItemDto(
    Guid PublicUserId,
    AdminPublicUserTelegramDto Telegram,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastLoginAt,
    Guid? StudentId,
    string? FullName,
    string? Phone,
    int? Age,
    int? Grade,
    AdminPublicUserAssessmentCountsDto Assessments,
    /// <summary>O'chirilgan bo'lsa vaqt, aks holda `null` (2026-09-08).</summary>
    DateTimeOffset? DeletedAt,
    /// <summary>`PublicUserDeletionReason` enum nomi (`ToString()`), o'chirilmagan bo'lsa `null`.</summary>
    string? DeletionReason,
    /// <summary>Erkin matnli izoh, o'chirilmagan yoki yozilmagan bo'lsa `null`.</summary>
    string? DeletionComment,
    AdminPublicUserLastAssessmentDto? LastAssessment);

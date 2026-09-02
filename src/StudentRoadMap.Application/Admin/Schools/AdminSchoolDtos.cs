namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// `GET /api/admin/schools` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.1-bo'lim:
/// `id, name, region, district, slug, publicUrl, isActive, studentCount, completedCount,
/// lastActivityAt`.
/// </summary>
public sealed record AdminSchoolListItemDto(
    Guid Id,
    string Name,
    string Region,
    string District,
    string Slug,
    string PublicUrl,
    bool IsActive,
    int StudentCount,
    int CompletedCount,
    DateTimeOffset? LastActivityAt);

/// <summary>
/// `GET /api/admin/schools/{id}` — batafsil + statistika (`docs/07` 3.1-bo'lim: "o'quvchi soni,
/// yakunlangan sessiyalar, oxirgi faollik"). `AccessCode` ko'rinadi (admin katalogi) —
/// `AccessToken`ning o'zi emas, faqat `publicUrl` ichida (havolani ko'chirish uchun yetarli).
///
/// **`QrCodeBase64`** — P23 (admin maktablar UI) talabi: jadvaldagi QR ikonkasi bosilganda
/// havolani O'ZGARTIRMASDAN (`RegenerateLink` chaqirmasdan) joriy havolaning QR kodini
/// ko'rsatishi kerak — aks holda admin QR ko'rish uchun havolani majburan yangilashga
/// majbur bo'lardi, bu esa o'sha maktabning barcha faol sessiyalarini (eski havola bo'yicha)
/// yarim yo'lda qoldirardi. `docs/07` 3.1 rasman faqat `regenerate-link` javobida QR ko'rsatadi,
/// lekin bu — o'qish (`GetById`) uchun zararsiz kengaytma (yangi maxfiy ma'lumot chiqarilmaydi,
/// QR shunchaki `publicUrl`ning grafik ko'rinishi). PM'ga savol: `docs/07`ga rasman kiritilsinmi?
/// </summary>
public sealed record AdminSchoolDetailDto(
    Guid Id,
    string Name,
    string Region,
    string District,
    string? SchoolNumber,
    string? ContactPerson,
    string? ContactPhone,
    string Slug,
    string PublicUrl,
    string QrCodeBase64,
    string? AccessCode,
    int DailyRegistrationLimit,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AdminSchoolStatsDto Stats);

/// <summary>
/// Maktab ichki sahifasidagi ishtirok statistikasi (`docs/07` 3.1-bo'lim).
///
/// - `StudentCount`     — shu maktabda ro'yxatdan o'tgan o'quvchilar (soft-delete qilinganlar
///   HISOBGA OLINMAYDI — `Student` global query filtri `!IsDeleted`).
/// - `CompletedCount`   — kamida bitta sessiyani TO'LIQ yakunlagan o'quvchilar
///   (`Student.CompletedAssessmentCount > 0` snapshot ustuni — `Assessments`ga tegilmaydi).
/// - `InProgressCount`  — hozir JARAYONDA bo'lgan sessiyalar: `Status == InProgress` (boshlangan,
///   lekin yakunlanmagan). `Draft` (hali bironta test boshlanmagan) va `Abandoned` KIRMAYDI —
///   `AdminDashboardFunnelDto.Started` ("`Status != Draft`") bilan bir xil ruhda.
/// - `CompletionRate`   — **ULUSH (0..1), foiz EMAS** (`AdminDashboardMath.CompletionRate` bilan
///   BIR XIL birlik va qoida: `StudentCount == 0` bo'lsa `null`, `0` emas — "hali hech kim
///   ro'yxatdan o'tmagan" bilan "HAQIQIY 0%" chalkashtirilmasin, `docs/06` §8, 2026-09-02 qaror).
///   Frontend ko'rsatishdan oldin `× 100` qiladi.
/// - `LastActivityAt`   — o'quvchilar snapshotidagi eng so'nggi `LastAssessmentAt` (hech kim
///   topshirmagan bo'lsa `null`).
/// </summary>
public sealed record AdminSchoolStatsDto(
    int StudentCount,
    int CompletedCount,
    int InProgressCount,
    double? CompletionRate,
    DateTimeOffset? LastActivityAt);

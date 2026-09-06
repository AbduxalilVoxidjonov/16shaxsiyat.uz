namespace StudentRoadMap.Application.Admin.Programs;

/// <summary>
/// `GET /api/admin/programs` ro'yxat elementi — `prompts/34` E15-band (minimal admin API).
/// `scale`/`scaleDirection` bu yerda umuman yo'q — dastur darajasida bunday maydon mavjud emas.
///
/// **2026-09-06:** `status` va `isActive` maydonlari javobdan OLIB TASHLANDI, o'rniga bitta
/// `state` (`ProgramState`: `Draft` · `Active` · `Paused` · `Archived`) beriladi. Sabab —
/// egasi bitta dasturni bir vaqtda "Arxiv" ham, "Faol" ham deb ko'rgan edi: ikkita mustaqil
/// maydon mijozga ikkita mustaqil belgi sifatida chiqardi. Juftlikni javobda qoldirish shu
/// xatoni istalgan mijozda qayta yaratish imkonini beradi, hosila holat esa hech qanday
/// ma'lumot yo'qotmaydi (`Draft`da `IsActive` ma'nosiz, `Archived`da u DOIM `false`).
/// Orqaga moslik saqlanmadi: bu ICHKI admin API va uning yagona mijozi — shu repodagi
/// frontend (`docs/07` 3.5).
/// </summary>
public sealed record AdminProgramListItemDto(
    Guid Id,
    string Code,
    string NameUz,
    string Kind,
    string Visibility,
    string State,
    bool IsSystem,
    int DisplayOrder,
    int TestCount);

/// <summary>
/// `GET /api/admin/programs/{id}` — batafsil: tarkib (testlar) va biriktirilgan maktablar.
/// `state` — ro'yxat DTO'sidagi bilan AYNAN bir xil hosila holat (yuqoridagi izohga qarang).
/// </summary>
public sealed record AdminProgramDetailDto(
    Guid Id,
    string Code,
    string NameUz,
    string? DescriptionUz,
    string Kind,
    string Visibility,
    string State,
    bool IsSystem,
    int DisplayOrder,
    IReadOnlyList<AdminProgramTestItemDto> Tests,
    IReadOnlyList<Guid> AssignedSchoolIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Dastur tarkibidagi bitta anketa — `docs/07` uslubida `scale`/`scaleDirection`siz.</summary>
public sealed record AdminProgramTestItemDto(Guid TestDefinitionId, string Code, string NameUz, int DisplayOrder);

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>
/// `GET`/`PUT /api/admin/catalog/tests/{id}/assignment` javobi (2026-09-23 egasi qarori,
/// `docs/07` §3.4.1, `docs/18` §9.7) — test KIMGA ochiq: ommaviy (hamma uchun) yoki
/// alohida maktablarga biriktirilgan. Ichkarida — test dasturi (`TestPrograms`), lekin
/// "dastur" tushunchasi bu shartnomada yo'q (`ProgramId` ham qaytarilmaydi).
///
/// <list type="bullet">
///   <item>`IsConfigured` — test dasturi mavjudmi. `false` bo'lsa qolgan maydonlar standart
///   (bo'sh) qiymatlarda: `IsPublic = false`, `SchoolIds = []`, `RegistrationMode = "Full"`,
///   `State = null`.</item>
///   <item>`IsPublic` — `true`: test BARCHA maktab havolalarida va ommaviy kabinetda ko'rinadi
///   (`ProgramVisibility.Public`). `false`: faqat `SchoolIds` (va `IsInPublicSpace`).</item>
///   <item>`SchoolIds` — aniq biriktirilgan MAKTABLAR (`SchoolKind.School`), ommaviy makon
///   bu ro'yxatga kirmaydi.</item>
///   <item>`IsInPublicSpace` — test ommaviy makonga (kabinetga) alohida biriktirilganmi
///   (`IsPublic = false` bo'lsa ham kabinetda ko'rinishi uchun; "Ommaviy makon" bo'limi bilan
///   bir xil yozuv).</item>
///   <item>`State` — ichki holat (`Draft`·`Active`·`Paused`·`Archived`), test holatidan
///   avtomatik kelib chiqadi: test `Published` + faol bo'lsagina `Active`.</item>
///   <item>`IsAvailable` — o'quvchilar hozir shu testni boshlay oladimi: `State == Active` VA
///   kamida bitta yo'nalish (`IsPublic` / `SchoolIds` / `IsInPublicSpace`) bor.</item>
///   <item>`SessionCount` — shu biriktirish orqali ochilgan sessiyalar soni (tarix).</item>
/// </list>
/// </summary>
public sealed record AdminTestAssignmentDto(
    Guid TestDefinitionId,
    string TestStatus,
    bool TestIsActive,
    bool IsConfigured,
    bool IsPublic,
    IReadOnlyList<Guid> SchoolIds,
    bool IsInPublicSpace,
    string RegistrationMode,
    string? State,
    bool IsAvailable,
    bool HasPersonalityBattery,
    int SessionCount);

namespace StudentRoadMap.Application.Public.GetTypeCatalog;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.10-bo'lim `200` javob shakli. Massiv emas, obyekt — qolgan
/// ommaviy javoblar bilan bir xil (`GetSchoolInfoResult`, `GetTestQuestionsResult`) va keyinchalik
/// yon maydon (masalan `updatedAt`) qo'shilsa mijoz shartnomasi buzilmaydi.
/// </summary>
public sealed record GetTypeCatalogResult(IReadOnlyList<PublicTypeCatalogItemDto> Types);

/// <summary>
/// Bitta shaxsiyat tipi — matnning HAMMASI `SeedData/type-catalog.json` dan (loyihaning o'z
/// o'zbekcha kontenti, `CLAUDE.md` 6a-qoida: tashqi manbadan ko'chirilmaydi).
///
/// `Code` — jadval kaliti (masalan `"INTJ"`), mijozda URL segmenti sifatida ham ishlatiladi
/// (`/metodika/intj`). Bu yerda ballar, shkala yoki yo'nalish YO'Q (`CLAUDE.md` 9-qoida) —
/// javob butunlay tavsifiy.
/// </summary>
public sealed record PublicTypeCatalogItemDto(
    string Code,
    string Name,
    string ShortDescription,
    string LongDescription,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> GrowthAreas,
    IReadOnlyList<string> CareerHints);

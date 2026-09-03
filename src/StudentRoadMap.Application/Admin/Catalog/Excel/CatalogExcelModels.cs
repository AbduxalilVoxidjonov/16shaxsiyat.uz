namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// Excel (`.xlsx`) shablon/eksport/import uchun YAGONA model — `docs/07` §3.4 "Excel shablon"
/// bloki. Maydonlari ATAYLAB `frontend/src/features/catalog/model/importSchema.ts` dagi JSON
/// import sxemasi bilan bir xil nomda: `POST /api/admin/catalog/import/parse-excel` shu
/// obyektni qaytaradi va frontend uni O'ZGARISHSIZ mavjud oldindan ko'rish + yaratish yo'liga
/// beradi (ikkita parallel import mantiqi paydo bo'lmasin).
///
/// <para>
/// <see cref="Scales"/> JSON sxemasida yo'q edi — Excel'da bor, chunki nashr validatsiyasi
/// (`docs/03` §6.3) talqin oraliqlarini TALAB qiladi: oraliqsiz import qilingan anketani
/// nashr qilib bo'lmaydi. Shu sabab shkala va oraliqlar faylning bir qismi.
/// </para>
/// </summary>
public sealed record CatalogExcelTestDto(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int EstimatedMinutes,
    int PageSize,
    string ScoringMode,
    IReadOnlyList<CatalogExcelScaleDto> Scales,
    IReadOnlyList<CatalogExcelQuestionDto> Questions);

/// <summary>`Shkalalar` + `Oraliqlar` varaqlaridan yig'iladi (shkala kodi bo'yicha).</summary>
public sealed record CatalogExcelScaleDto(
    string Code,
    string NameUz,
    string? DescriptionUz,
    IReadOnlyList<InterpretationBandDto> InterpretationBands);

/// <summary>`Savollar` varag'ining bitta qatori — JSON import sxemasidagi savol bilan bir xil shakl.</summary>
public sealed record CatalogExcelQuestionDto(
    string Code,
    int Order,
    string TextUz,
    string Type,
    string Scale,
    int Direction,
    decimal Weight,
    bool IsRequired);

/// <summary>
/// Faylni o'qishda topilgan MUAMMO — faylni butunlay rad etmaydi (u uchun
/// <see cref="CatalogExcelParseOutcome.FileErrorMessage"/> bor), balki superadminga
/// nimani tuzatish kerakligini aytadi. Shakli `importSchema.ts` dagi `ImportIssue` bilan
/// mos (`code`, `message`, ixtiyoriy `questionCode`/`scale`) + Excel'ga xos `sheet`/`row`.
/// </summary>
public sealed record CatalogExcelIssueDto(
    string Code,
    string Message,
    string? Sheet = null,
    int? Row = null,
    string? QuestionCode = null,
    string? Scale = null);

/// <summary>`POST /api/admin/catalog/import/parse-excel` javobi — hech narsa SAQLANMAYDI.</summary>
public sealed record ParseCatalogExcelResultDto(
    CatalogExcelTestDto? Data,
    IReadOnlyList<CatalogExcelIssueDto> Issues);

/// <summary>Yuklab olinadigan fayl (`export.xlsx` / `import-template.xlsx`).</summary>
public sealed record CatalogExcelFileDto(byte[] Content, string FileName, string ContentType);

/// <summary>
/// Parser natijasi. <see cref="FileErrorMessage"/> <c>null</c> BO'LMASA — fayl umuman
/// o'qilmadi (zip emas, buzilgan, `.xls`/`.xlsm`, chegaradan katta): chaqiruvchi `400` beradi.
/// Xabar HAR DOIM oldindan yozilgan o'zbekcha matnlardan biri — kutubxona xabari, stack trace
/// yoki fayl yo'li HECH QACHON ichiga tushmaydi (P31 naqshi).
/// </summary>
public sealed record CatalogExcelParseOutcome(
    string? FileErrorMessage,
    CatalogExcelTestDto? Data,
    IReadOnlyList<CatalogExcelIssueDto> Issues)
{
    public static CatalogExcelParseOutcome FileError(string message) =>
        new(message, null, []);
}

/// <summary>
/// Yuklash chegaralari — `.xlsx` ZIP arxiv bo'lgani uchun himoyasiz endpoint butun API'ni
/// (ya'ni BARCHA maktabni) bo'g'ib qo'yishi mumkin.
/// </summary>
public static class CatalogExcelLimits
{
    /// <summary>Fayl hajmi — oshsa `413 PAYLOAD_TOO_LARGE`.</summary>
    public const long MaxFileBytes = 2 * 1024 * 1024;

    /// <summary>Bitta varaqdagi ma'lumot qatorlari (savol/shkala/oraliq) chegarasi.</summary>
    public const int MaxRows = 500;

    /// <summary>
    /// ZIP ichidagi yoyilgan (dekompressiya qilingan) umumiy hajm chegarasi — "zip bomba"
    /// himoyasi. Markaziy katalogdagi e'lon qilingan hajm hujumchi nazoratida bo'lsa-da,
    /// bu ARZON birinchi to'siq: ClosedXML faylni to'liq XOTIRAGA yoyadi, shu sabab yoyish
    /// BOSHLANMASDAN oldin tekshiriladi. Ikkinchi to'siq — <see cref="MaxRows"/>.
    /// </summary>
    public const long MaxUncompressedBytes = 20 * 1024 * 1024;

    /// <summary>ZIP ichidagi yozuvlar soni chegarasi (ko'p mayda yozuv bilan hujum).</summary>
    public const int MaxZipEntries = 512;

    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

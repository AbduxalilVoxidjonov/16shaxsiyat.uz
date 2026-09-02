namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `.xlsx` fayl generatsiyasi abstraksiyasi (`prompts/27-eksport-excel-va-pdf.md`) —
/// `Infrastructure/Export/ExcelExporter` `ClosedXML` orqali amalga oshiradi (`docs/06`da
/// ko'rsatilgan kutubxona). `Application` qatlami `ClosedXML` paketiga bog'lanmaydi
/// (`docs/06-arxitektura.md` 3-bo'lim).
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// `sheetName` — varaq nomi. `columns` — sarlavha qatori (tartib bilan; muzlatiladi,
    /// avtofiltr qo'yiladi, ustun kengligi avtomatik moslashadi). `rows` — `IAsyncEnumerable`:
    /// chaqiruvchi qatorlarni BITTA-BITTA (masalan, DB'dan bo'laklab — batch-batch — o'qiladigan
    /// generator orqali) beradi, shu bilan butun natija oldindan xotiraga to'liq ro'yxat
    /// sifatida yig'ilmaydi (`prompts/27` MAXSUS DIQQAT #2: "Xotirada butun jadval
    /// yig'ilmasin"). Har qator uzunligi `columns.Count`ga teng bo'lishi kerak.
    /// </summary>
    Task<byte[]> ExportAsync(
        string sheetName,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<IReadOnlyList<ExcelCellValue>> rows,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Excel katagi qiymati — turi ANIQ ko'rsatiladi, shu bilan masalan telefon raqami HECH QACHON
/// sonli/sana formatga aylantirilmaydi (`prompts/27` cheklovi: "Excel'da telefon raqamlari
/// matn sifatida (formatlanmasin)").
/// </summary>
public abstract record ExcelCellValue
{
    private ExcelCellValue()
    {
    }

    /// <summary>Matn katagi — har doim `Text` (`"@"`) formatida, `ClosedXML` sonni "taxmin qilmaydi".</summary>
    public sealed record Text(string? Value) : ExcelCellValue;

    /// <summary>Sonli katak — Excelda saralash/filtrlash sonli ustun sifatida ishlaydi.</summary>
    public sealed record Number(double? Value) : ExcelCellValue;

    /// <summary>Sana katagi — Excel sana formatida ko'rsatiladi.</summary>
    public sealed record Date(DateTimeOffset? Value) : ExcelCellValue;

    public static ExcelCellValue Of(string? value) => new Text(value);

    public static ExcelCellValue Of(double? value) => new Number(value);

    public static ExcelCellValue Of(DateTimeOffset? value) => new Date(value);
}

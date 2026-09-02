using ClosedXML.Excel;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Export;

/// <summary>
/// `IExcelExporter` — `ClosedXML` orqali (`docs/06`da ko'rsatilgan kutubxona, `prompts/27`
/// MAXSUS DIQQAT #7). Bitta chaqiruv — statsiz (holatsiz), `Singleton` sifatida ro'yxatdan
/// o'tkaziladi (`DependencyInjection`).
///
/// **Nega "haqiqiy streaming" emas:** `.xlsx` — ZIP ichidagi XML fayllar to'plami; `ClosedXML`
/// (boshqa mashhur .NET Excel kutubxonalari kabi) yakuniy workbook obyekt grafigini xotirada
/// quradi, keyin bir yo'la saqlaydi — qatorma-qator diskka yozib boradigan "true streaming
/// writer" emas. Xotira muammosi shu SABABDAN emas — u DB'DAN butun filtrlangan jadvalni
/// BITTA `ToListAsync()` bilan yuklashdan kelib chiqadi (`docs/06` §8, P14 qarori); shuni oldini
/// olish uchun chaqiruvchi (`ExportStudentsQueryHandler`) qatorlarni DB'dan BO'LAKLAB
/// (`IAsyncEnumerable`) beradi — bu klass ularni BITTA-BITTA workbook'ga qo'shib boradi.
/// </summary>
internal sealed class ExcelExporter : IExcelExporter
{
    public async Task<byte[]> ExportAsync(
        string sheetName,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<IReadOnlyList<ExcelCellValue>> rows,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).SetValue(columns[columnIndex]);
        }

        // Sarlavha qatori — qalin, muzlatilgan (`prompts/27` vazifa #1).
        worksheet.Row(1).Style.Font.SetBold();
        worksheet.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E4ECF7"));
        worksheet.SheetView.FreezeRows(1);

        var rowIndex = 1;
        await foreach (var row in rows.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            rowIndex++;
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                SetCell(worksheet.Cell(rowIndex, columnIndex + 1), row[columnIndex]);
            }
        }

        // Avtofiltr + avtomatik ustun kengligi — faqat ma'lumot bo'lsa (bo'sh eksportda
        // `RangeUsed()` faqat sarlavha qatorini qamrab oladi, baribir zararsiz).
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCell(IXLCell cell, ExcelCellValue value)
    {
        switch (value)
        {
            case ExcelCellValue.Text text:
                // `ClosedXML` (`XLCellValue`, 0.100+) `string` qiymatini HAR DOIM `Text` turi
                // sifatida saqlaydi (kontentni "taxmin qilib" sonli/sana turiga aylantirmaydi) —
                // shu sabab telefon raqami HECH QACHON avtomatik sonli formatga aylanmaydi
                // (`prompts/27` cheklovi). `"@"` — faqat Excel ochilganda ko'rinadigan format,
                // qo'shimcha kafolat.
                cell.Style.NumberFormat.SetFormat("@");
                cell.SetValue(text.Value ?? string.Empty);
                break;

            case ExcelCellValue.Number number:
                if (number.Value.HasValue)
                {
                    cell.SetValue(number.Value.Value);
                }

                break;

            case ExcelCellValue.Date date:
                if (date.Value.HasValue)
                {
                    cell.Style.DateFormat.SetFormat("yyyy-mm-dd");
                    cell.SetValue(date.Value.Value.UtcDateTime);
                }

                break;

            default:
                throw new NotSupportedException($"Noma'lum Excel katak turi: {value.GetType().Name}");
        }
    }
}

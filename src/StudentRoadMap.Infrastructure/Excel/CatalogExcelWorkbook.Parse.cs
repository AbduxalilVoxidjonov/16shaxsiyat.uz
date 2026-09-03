using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Excel;

namespace StudentRoadMap.Infrastructure.Excel;

/// <summary>
/// Anketa `.xlsx` faylini O'QISH. `.xlsx` — bu ZIP arxiv, ya'ni yuklash endpointi
/// himoyasiz qolsa butun API'ni (barcha maktabni) bo'g'ib qo'yishi mumkin. Shu sabab
/// ClosedXML'ga BERISHDAN OLDIN arxivning o'zi tekshiriladi:
/// <list type="number">
///   <item><description>hajm chegarasi (<see cref="CatalogExcelLimits.MaxFileBytes"/>);</description></item>
///   <item><description>sehrli baytlar — fayl ROSTDAN zip (`PK\x03\x04`) ekani; eski ikkilik `.xls` alohida, tushunarli xabar bilan rad etiladi;</description></item>
///   <item><description>yoyilgan umumiy hajm va yozuvlar soni (zip bomba) — dekompressiya BOSHLANMASDAN;</description></item>
///   <item><description>makro (`.xlsm`) — `xl/vbaProject.bin` yoki `macroEnabled` content-type;</description></item>
///   <item><description>Open XML ekani — `xl/workbook.xml` mavjudligi;</description></item>
///   <item><description>qator soni (<see cref="CatalogExcelLimits.MaxRows"/>) — arxiv ichidagi katta yoyilishga ikkinchi to'siq.</description></item>
/// </list>
///
/// <para>
/// <b>Xato matnlari.</b> Barcha xabar shu fayldagi O'ZGARMAS o'zbekcha satrlardan biri —
/// istisno xabari, stack trace, kutubxona matni yoki fayl yo'li HECH QACHON javobga
/// tushmaydi (P31 da butun API bo'ylab tuzatilgan naqsh). Shu sabab har bir `catch` bloki
/// istisnoni YUTADI va oldindan yozilgan xabarni qaytaradi.
/// </para>
/// </summary>
internal sealed partial class CatalogExcelWorkbook
{
    private const string NotExcelMessage =
        "Fayl .xlsx (Excel Workbook) formatida emas. Faylni Excel'da \"Farqli saqlash\" orqali .xlsx sifatida saqlab, qayta urinib ko'ring.";

    private const string LegacyXlsMessage =
        "Eski .xls format qabul qilinmaydi. Faylni Excel'da \"Farqli saqlash\" → \"Excel Workbook (.xlsx)\" sifatida saqlang.";

    private const string MacroMessage =
        "Makrolar bilan fayl (.xlsm) qabul qilinmaydi. Faylni makrosiz .xlsx sifatida saqlang.";

    private const string CorruptMessage =
        "Faylni o'qib bo'lmadi — u buzilgan yoki .xlsx emas. Faylni qayta saqlab, yana urinib ko'ring.";

    private const string EmptyFileMessage = "Fayl bo'sh.";

    private static readonly string TooLargeMessage =
        $"Fayl juda katta. Ruxsat etilgan hajm — {CatalogExcelLimits.MaxFileBytes / (1024 * 1024)} MB.";

    private static readonly string TooManyRowsMessage =
        $"Faylda juda ko'p qator bor. Bitta varaqda ko'pi bilan {CatalogExcelLimits.MaxRows} ta qator bo'lishi mumkin.";

    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04];

    private static readonly byte[] LegacyXlsMagic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public CatalogExcelParseOutcome Parse(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var archiveError = ValidateArchive(content);
        if (archiveError is not null)
        {
            return CatalogExcelParseOutcome.FileError(archiveError);
        }

        XLWorkbook workbook;
        try
        {
            using var stream = new MemoryStream(content, writable: false);

            // `RecalculateAllFormulas = false` — formulalar HISOBLANMAYDI, Excel saqlagan
            // keshlangan qiymat o'qiladi (`ReadCell`). Hisoblash yuklangan faylga protsessor
            // vaqti sarflashga ruxsat berish demakdir.
            workbook = new XLWorkbook(stream, new LoadOptions { RecalculateAllFormulas = false });
        }
        catch (Exception)
        {
            // Kutubxona istisnosi ATAYLAB yutiladi — xabari javobga chiqmasligi kerak.
            return CatalogExcelParseOutcome.FileError(CorruptMessage);
        }

        try
        {
            return ReadWorkbook(workbook);
        }
        catch (Exception)
        {
            return CatalogExcelParseOutcome.FileError(CorruptMessage);
        }
        finally
        {
            workbook.Dispose();
        }
    }

    /// <summary>ClosedXML'ga berishdan OLDIN arxivning o'zini tekshiradi. Xato bo'lsa xabar, aks holda <c>null</c>.</summary>
    private static string? ValidateArchive(byte[] content)
    {
        if (content.Length == 0)
        {
            return EmptyFileMessage;
        }

        if (content.LongLength > CatalogExcelLimits.MaxFileBytes)
        {
            return TooLargeMessage;
        }

        if (StartsWith(content, LegacyXlsMagic))
        {
            return LegacyXlsMessage;
        }

        if (!StartsWith(content, ZipMagic))
        {
            // Kengaytmaga emas, MAZMUNGA ishonamiz: `.xlsx` deb nomlangan PDF/ZIP-bo'lmagan
            // fayl shu yerda to'xtaydi.
            return NotExcelMessage;
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.Entries.Count > CatalogExcelLimits.MaxZipEntries)
            {
                return CorruptMessage;
            }

            long uncompressed = 0;
            var hasWorkbook = false;
            string? contentTypesEntryName = null;

            foreach (var entry in archive.Entries)
            {
                uncompressed += entry.Length;
                if (uncompressed > CatalogExcelLimits.MaxUncompressedBytes)
                {
                    return TooLargeMessage;
                }

                if (entry.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                {
                    return MacroMessage;
                }

                if (string.Equals(entry.FullName, "xl/workbook.xml", StringComparison.OrdinalIgnoreCase))
                {
                    hasWorkbook = true;
                }

                if (string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    contentTypesEntryName = entry.FullName;
                }
            }

            if (!hasWorkbook || contentTypesEntryName is null)
            {
                return NotExcelMessage;
            }

            // `.xlsm` makro yoqilgan workbook sifatida `[Content_Types].xml` da e'lon qilinadi
            // — `vbaProject.bin` olib tashlangan bo'lsa ham shu yerda ushlanadi.
            var contentTypes = ReadSmallEntry(archive, contentTypesEntryName);
            if (contentTypes is not null && contentTypes.Contains("macroEnabled", StringComparison.OrdinalIgnoreCase))
            {
                return MacroMessage;
            }

            return null;
        }
        catch (InvalidDataException)
        {
            return CorruptMessage;
        }
        catch (Exception)
        {
            return CorruptMessage;
        }
    }

    /// <summary>`[Content_Types].xml` ni O'QIYDI, lekin faqat chegaralangan hajmda (yoyilish himoyasi).</summary>
    private static string? ReadSmallEntry(ZipArchive archive, string entryName)
    {
        const int MaxBytes = 256 * 1024;

        var entry = archive.GetEntry(entryName);
        if (entry is null || entry.Length > MaxBytes)
        {
            return null;
        }

        try
        {
            using var entryStream = entry.Open();
            var buffer = new byte[MaxBytes];
            var read = 0;
            int chunk;
            while (read < MaxBytes && (chunk = entryStream.Read(buffer, read, MaxBytes - read)) > 0)
            {
                read += chunk;
            }

            return Encoding.UTF8.GetString(buffer, 0, read);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool StartsWith(byte[] content, byte[] prefix)
    {
        if (content.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (content[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private static CatalogExcelParseOutcome ReadWorkbook(XLWorkbook workbook)
    {
        var issues = new List<CatalogExcelIssueDto>();

        var testSheet = FindSheet(workbook, CatalogExcelSchema.TestSheet, issues);
        var questionsSheet = FindSheet(workbook, CatalogExcelSchema.QuestionsSheet, issues);

        if (testSheet is null || questionsSheet is null)
        {
            return new CatalogExcelParseOutcome(null, null, issues);
        }

        foreach (var sheet in new[] { testSheet, questionsSheet })
        {
            if (DataRowCount(sheet) > CatalogExcelLimits.MaxRows)
            {
                return CatalogExcelParseOutcome.FileError(TooManyRowsMessage);
            }
        }

        var scalesSheet = workbook.Worksheets.FirstOrDefault(s => SheetNameMatches(s, CatalogExcelSchema.ScalesSheet));
        var bandsSheet = workbook.Worksheets.FirstOrDefault(s => SheetNameMatches(s, CatalogExcelSchema.BandsSheet));

        foreach (var sheet in new[] { scalesSheet, bandsSheet })
        {
            if (sheet is not null && DataRowCount(sheet) > CatalogExcelLimits.MaxRows)
            {
                return CatalogExcelParseOutcome.FileError(TooManyRowsMessage);
            }
        }

        var meta = ReadTestRow(testSheet, issues);
        var questions = ReadQuestions(questionsSheet, issues);
        var scales = ReadScales(scalesSheet, bandsSheet, issues);

        if (meta is null)
        {
            return new CatalogExcelParseOutcome(null, null, issues);
        }

        var data = meta with { Scales = scales, Questions = questions };

        return new CatalogExcelParseOutcome(null, data, issues);
    }

    private static IXLWorksheet? FindSheet(XLWorkbook workbook, string name, List<CatalogExcelIssueDto> issues)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(s => SheetNameMatches(s, name));
        if (sheet is null)
        {
            issues.Add(new CatalogExcelIssueDto(
                "SHEET_MISSING",
                $"\"{name}\" varag'i topilmadi.",
                Sheet: name));
        }

        return sheet;
    }

    private static bool SheetNameMatches(IXLWorksheet sheet, string name) =>
        string.Equals(CatalogExcelSchema.NormalizeHeader(sheet.Name), CatalogExcelSchema.NormalizeHeader(name), StringComparison.Ordinal);

    private static int DataRowCount(IXLWorksheet sheet) => Math.Max(0, LastRow(sheet) - 1);

    private static int LastRow(IXLWorksheet sheet) => sheet.LastRowUsed()?.RowNumber() ?? 0;

    /// <summary>
    /// Sarlavha qatoridan ustun nomi → ustun raqami xaritasi. Ustun TARTIBI ahamiyatsiz;
    /// yetishmagan ustun aniq xabar bilan bildiriladi (`COLUMN_MISSING`).
    /// </summary>
    private static Dictionary<string, int> ReadHeader(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = CatalogExcelSchema.NormalizeHeader(ReadCell(sheet.Cell(1, column)));
            if (header.Length > 0 && !map.ContainsKey(header))
            {
                map[header] = column;
            }
        }

        return map;
    }

    private static int? Column(Dictionary<string, int> header, string name) =>
        header.TryGetValue(CatalogExcelSchema.NormalizeHeader(name), out var column) ? column : null;

    private static int? RequireColumn(Dictionary<string, int> header, string sheetName, string name, List<CatalogExcelIssueDto> issues)
    {
        var column = Column(header, name);
        if (column is null)
        {
            issues.Add(new CatalogExcelIssueDto(
                "COLUMN_MISSING",
                $"\"{sheetName}\" varag'ida \"{name}\" ustuni topilmadi.",
                Sheet: sheetName));
        }

        return column;
    }

    private static string Text(IXLWorksheet sheet, int row, int? column) =>
        column is null ? string.Empty : ReadCell(sheet.Cell(row, column.Value));

    /// <summary>
    /// Katak qiymati MATN sifatida. Formulali katakda Excel saqlagan KESHLANGAN qiymat
    /// olinadi — formula qayta hisoblanmaydi. Xato qiymat (`#REF!`) bo'sh deb qaraladi.
    /// </summary>
    private static string ReadCell(IXLCell cell)
    {
        XLCellValue value;
        try
        {
            value = cell.HasFormula ? cell.CachedValue : cell.Value;
        }
        catch (Exception)
        {
            return string.Empty;
        }

        return value.Type switch
        {
            XLDataType.Blank => string.Empty,
            XLDataType.Text => value.GetText().Trim(),
            XLDataType.Number => value.GetNumber().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => value.GetBoolean() ? "true" : "false",
            XLDataType.DateTime => value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            XLDataType.TimeSpan => value.GetTimeSpan().ToString(),
            _ => string.Empty,
        };
    }

    private static CatalogExcelTestDto? ReadTestRow(IXLWorksheet sheet, List<CatalogExcelIssueDto> issues)
    {
        var header = ReadHeader(sheet);
        var sheetName = CatalogExcelSchema.TestSheet;

        var codeColumn = RequireColumn(header, sheetName, CatalogExcelSchema.TestCode, issues);
        var nameColumn = RequireColumn(header, sheetName, CatalogExcelSchema.TestName, issues);
        var minutesColumn = RequireColumn(header, sheetName, CatalogExcelSchema.TestMinutes, issues);
        var descriptionColumn = Column(header, CatalogExcelSchema.TestDescription);
        var pageSizeColumn = Column(header, CatalogExcelSchema.TestPageSize);
        var scoringColumn = Column(header, CatalogExcelSchema.TestScoringMode);

        if (codeColumn is null || nameColumn is null || minutesColumn is null)
        {
            return null;
        }

        if (LastRow(sheet) < 2)
        {
            issues.Add(new CatalogExcelIssueDto(
                "TEST_ROW_MISSING",
                $"\"{sheetName}\" varag'i bo'sh — 2-qatorga anketa ma'lumotlarini yozing.",
                Sheet: sheetName));
            return null;
        }

        var code = Text(sheet, 2, codeColumn);
        var name = Text(sheet, 2, nameColumn);
        var description = Text(sheet, 2, descriptionColumn);
        var minutesText = Text(sheet, 2, minutesColumn);
        var pageSizeText = Text(sheet, 2, pageSizeColumn);
        var scoringMode = Text(sheet, 2, scoringColumn);

        if (code.Length == 0)
        {
            issues.Add(new CatalogExcelIssueDto("TEST_CODE_MISSING", "Anketa kodi kiritilmagan.", Sheet: sheetName, Row: 2));
        }

        if (name.Length == 0)
        {
            issues.Add(new CatalogExcelIssueDto("TEST_NAME_MISSING", "Anketa nomi kiritilmagan.", Sheet: sheetName, Row: 2));
        }

        var minutes = ParseInt(minutesText);
        if (minutes is null or <= 0)
        {
            issues.Add(new CatalogExcelIssueDto(
                "TEST_MINUTES_INVALID",
                $"\"{CatalogExcelSchema.TestMinutes}\" musbat butun son bo'lishi kerak.",
                Sheet: sheetName,
                Row: 2));
        }

        var pageSize = ParseInt(pageSizeText);
        if (pageSizeText.Length > 0 && pageSize is null or <= 0)
        {
            issues.Add(new CatalogExcelIssueDto(
                "TEST_PAGE_SIZE_INVALID",
                $"\"{CatalogExcelSchema.TestPageSize}\" musbat butun son bo'lishi kerak.",
                Sheet: sheetName,
                Row: 2));
        }

        if (scoringMode.Length == 0)
        {
            scoringMode = "Scored";
        }
        else if (!string.Equals(scoringMode, "Scored", StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(scoringMode, "Survey", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new CatalogExcelIssueDto(
                "TEST_SCORING_MODE_INVALID",
                $"\"{CatalogExcelSchema.TestScoringMode}\" faqat \"Scored\" yoki \"Survey\" bo'lishi mumkin.",
                Sheet: sheetName,
                Row: 2));
            scoringMode = "Scored";
        }
        else
        {
            scoringMode = string.Equals(scoringMode, "Survey", StringComparison.OrdinalIgnoreCase) ? "Survey" : "Scored";
        }

        return new CatalogExcelTestDto(
            code,
            name,
            description.Length == 0 ? null : description,
            minutes is null or <= 0 ? 1 : minutes.Value,
            pageSize is null or <= 0 ? 10 : pageSize.Value,
            scoringMode,
            [],
            []);
    }

    private static IReadOnlyList<CatalogExcelQuestionDto> ReadQuestions(IXLWorksheet sheet, List<CatalogExcelIssueDto> issues)
    {
        var header = ReadHeader(sheet);
        var sheetName = CatalogExcelSchema.QuestionsSheet;

        var codeColumn = RequireColumn(header, sheetName, CatalogExcelSchema.QuestionCode, issues);
        var orderColumn = RequireColumn(header, sheetName, CatalogExcelSchema.QuestionOrder, issues);
        var textColumn = RequireColumn(header, sheetName, CatalogExcelSchema.QuestionText, issues);
        var scaleColumn = RequireColumn(header, sheetName, CatalogExcelSchema.QuestionScale, issues);
        var directionColumn = RequireColumn(header, sheetName, CatalogExcelSchema.QuestionDirection, issues);
        var weightColumn = Column(header, CatalogExcelSchema.QuestionWeight);
        var requiredColumn = Column(header, CatalogExcelSchema.QuestionRequired);
        var typeColumn = Column(header, CatalogExcelSchema.QuestionType);

        if (codeColumn is null || orderColumn is null || textColumn is null || scaleColumn is null || directionColumn is null)
        {
            return [];
        }

        var questions = new List<CatalogExcelQuestionDto>();
        var lastRow = LastRow(sheet);

        for (var row = 2; row <= lastRow; row++)
        {
            var code = Text(sheet, row, codeColumn);
            var text = Text(sheet, row, textColumn);
            var scale = Text(sheet, row, scaleColumn);
            var orderText = Text(sheet, row, orderColumn);
            var directionText = Text(sheet, row, directionColumn);
            var weightText = Text(sheet, row, weightColumn);
            var requiredText = Text(sheet, row, requiredColumn);
            var typeText = Text(sheet, row, typeColumn);

            // Butunlay bo'sh qator — jimgina o'tkazib yuboriladi (Excel'da odatiy hol).
            if (code.Length == 0 && text.Length == 0 && scale.Length == 0 && orderText.Length == 0 && directionText.Length == 0)
            {
                continue;
            }

            var rowIsValid = true;

            if (code.Length == 0)
            {
                issues.Add(Issue("QUESTION_CODE_MISSING", $"{row}-qatorda savol kodi yo'q.", sheetName, row, null));
                rowIsValid = false;
            }

            if (text.Length == 0)
            {
                issues.Add(Issue("QUESTION_TEXT_MISSING", $"{row}-qatorda savol matni yo'q.", sheetName, row, code));
                rowIsValid = false;
            }

            if (scale.Length == 0)
            {
                issues.Add(Issue("QUESTION_SCALE_MISSING", $"{row}-qatorda shkala kodi yo'q.", sheetName, row, code));
                rowIsValid = false;
            }

            var order = ParseInt(orderText);
            if (order is null or <= 0)
            {
                issues.Add(Issue("QUESTION_ORDER_INVALID", $"{row}-qatorda tartib raqami musbat butun son emas.", sheetName, row, code));
                rowIsValid = false;
            }

            var direction = ParseDirection(directionText);
            if (direction is null)
            {
                issues.Add(Issue("QUESTION_DIRECTION_INVALID", $"{row}-qatorda yo'nalish faqat 1 yoki -1 bo'lishi mumkin.", sheetName, row, code));
                rowIsValid = false;
            }

            decimal weight = 1m;
            if (weightText.Length > 0)
            {
                var parsedWeight = ParseDecimal(weightText);
                if (parsedWeight is null or <= 0m)
                {
                    issues.Add(Issue("QUESTION_WEIGHT_INVALID", $"{row}-qatorda og'irlik musbat son bo'lishi kerak.", sheetName, row, code));
                    rowIsValid = false;
                }
                else
                {
                    weight = parsedWeight.Value;
                }
            }

            var type = typeText.Length == 0 ? "Likert5" : NormalizeQuestionType(typeText);
            if (type is null)
            {
                issues.Add(Issue("QUESTION_TYPE_INVALID", $"{row}-qatorda javob turi faqat Likert5 yoki Likert7 bo'lishi mumkin.", sheetName, row, code));
                rowIsValid = false;
            }

            if (!rowIsValid)
            {
                continue;
            }

            questions.Add(new CatalogExcelQuestionDto(
                code,
                order!.Value,
                text,
                type!,
                scale,
                direction!.Value,
                weight,
                ParseBoolean(requiredText) ?? true));
        }

        if (questions.Count == 0 && issues.All(i => i.Code != "COLUMN_MISSING"))
        {
            issues.Add(new CatalogExcelIssueDto(
                "NO_QUESTIONS",
                $"\"{sheetName}\" varag'ida bironta savol topilmadi.",
                Sheet: sheetName));
        }

        return questions;
    }

    private static IReadOnlyList<CatalogExcelScaleDto> ReadScales(
        IXLWorksheet? scalesSheet,
        IXLWorksheet? bandsSheet,
        List<CatalogExcelIssueDto> issues)
    {
        if (scalesSheet is null)
        {
            return [];
        }

        var header = ReadHeader(scalesSheet);
        var sheetName = CatalogExcelSchema.ScalesSheet;

        var codeColumn = RequireColumn(header, sheetName, CatalogExcelSchema.ScaleCode, issues);
        var nameColumn = RequireColumn(header, sheetName, CatalogExcelSchema.ScaleName, issues);
        var descriptionColumn = Column(header, CatalogExcelSchema.ScaleDescription);

        if (codeColumn is null || nameColumn is null)
        {
            return [];
        }

        var ordered = new List<(string Code, string NameUz, string? DescriptionUz)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lastRow = LastRow(scalesSheet);

        for (var row = 2; row <= lastRow; row++)
        {
            var code = Text(scalesSheet, row, codeColumn);
            var name = Text(scalesSheet, row, nameColumn);
            var description = Text(scalesSheet, row, descriptionColumn);

            if (code.Length == 0 && name.Length == 0)
            {
                continue;
            }

            if (code.Length == 0)
            {
                issues.Add(new CatalogExcelIssueDto("SCALE_CODE_MISSING", $"{row}-qatorda shkala kodi yo'q.", Sheet: sheetName, Row: row));
                continue;
            }

            if (!seen.Add(code))
            {
                issues.Add(new CatalogExcelIssueDto("SCALE_CODE_DUPLICATE", $"Shkala kodi takrorlangan: {code}", Sheet: sheetName, Row: row, Scale: code));
                continue;
            }

            if (name.Length == 0)
            {
                issues.Add(new CatalogExcelIssueDto("SCALE_NAME_MISSING", $"\"{code}\" shkalasining nomi yo'q.", Sheet: sheetName, Row: row, Scale: code));
                name = code;
            }

            ordered.Add((code, name, description.Length == 0 ? null : description));
        }

        var bands = ReadBands(bandsSheet, seen, issues);

        return ordered
            .Select(s => new CatalogExcelScaleDto(
                s.Code,
                s.NameUz,
                s.DescriptionUz,
                bands.TryGetValue(s.Code, out var list) ? list : []))
            .ToList();
    }

    private static Dictionary<string, List<InterpretationBandDto>> ReadBands(
        IXLWorksheet? sheet,
        HashSet<string> knownScales,
        List<CatalogExcelIssueDto> issues)
    {
        var result = new Dictionary<string, List<InterpretationBandDto>>(StringComparer.Ordinal);
        if (sheet is null)
        {
            return result;
        }

        var header = ReadHeader(sheet);
        var sheetName = CatalogExcelSchema.BandsSheet;

        var codeColumn = RequireColumn(header, sheetName, CatalogExcelSchema.ScaleCode, issues);
        var fromColumn = RequireColumn(header, sheetName, CatalogExcelSchema.BandFrom, issues);
        var toColumn = RequireColumn(header, sheetName, CatalogExcelSchema.BandTo, issues);
        var labelColumn = RequireColumn(header, sheetName, CatalogExcelSchema.BandLabel, issues);

        if (codeColumn is null || fromColumn is null || toColumn is null || labelColumn is null)
        {
            return result;
        }

        var lastRow = LastRow(sheet);

        for (var row = 2; row <= lastRow; row++)
        {
            var code = Text(sheet, row, codeColumn);
            var fromText = Text(sheet, row, fromColumn);
            var toText = Text(sheet, row, toColumn);
            var label = Text(sheet, row, labelColumn);

            if (code.Length == 0 && fromText.Length == 0 && toText.Length == 0 && label.Length == 0)
            {
                continue;
            }

            if (code.Length == 0 || !knownScales.Contains(code))
            {
                issues.Add(new CatalogExcelIssueDto(
                    "BAND_SCALE_UNKNOWN",
                    $"{row}-qatordagi oraliq \"{CatalogExcelSchema.ScalesSheet}\" varag'idagi shkalaga tegishli emas.",
                    Sheet: sheetName,
                    Row: row,
                    Scale: code.Length == 0 ? null : code));
                continue;
            }

            var from = ParseDouble(fromText);
            var to = ParseDouble(toText);

            if (from is null || to is null)
            {
                issues.Add(new CatalogExcelIssueDto(
                    "BAND_BOUND_INVALID",
                    $"{row}-qatordagi oraliq chegaralari son emas.",
                    Sheet: sheetName,
                    Row: row,
                    Scale: code));
                continue;
            }

            if (label.Length == 0)
            {
                issues.Add(new CatalogExcelIssueDto(
                    "BAND_LABEL_MISSING",
                    $"{row}-qatordagi oraliqda yorliq yo'q.",
                    Sheet: sheetName,
                    Row: row,
                    Scale: code));
                continue;
            }

            // Butun son / bo'shliq / ustma-ust / to'liqlik qoidalari BU YERDA tekshirilmaydi —
            // ular `docs/03` §6.3 ning YAGONA nusxasida (`CatalogPublishValidator` va uning
            // frontend egizagi `interpretationBands.ts`) turadi. Bu yerda takrorlansa uchinchi
            // nusxa paydo bo'lardi.
            if (!result.TryGetValue(code, out var list))
            {
                list = [];
                result[code] = list;
            }

            list.Add(new InterpretationBandDto(from.Value, to.Value, label));
        }

        foreach (var list in result.Values)
        {
            list.Sort((a, b) => a.From.CompareTo(b.From));
        }

        return result;
    }

    private static CatalogExcelIssueDto Issue(string code, string message, string sheet, int row, string? questionCode) =>
        new(code, message, Sheet: sheet, Row: row, QuestionCode: string.IsNullOrEmpty(questionCode) ? null : questionCode);

    private static int? ParseInt(string value)
    {
        var number = ParseDouble(value);
        if (number is null || Math.Abs(number.Value - Math.Round(number.Value)) > 1e-9)
        {
            return null;
        }

        var rounded = Math.Round(number.Value);
        return rounded is >= int.MinValue and <= int.MaxValue ? (int)rounded : null;
    }

    private static double? ParseDouble(string value)
    {
        if (value.Length == 0)
        {
            return null;
        }

        // Excel katagi matn bo'lsa vergul (`1,5`) yoki unicode minus (`−1`) kelishi mumkin.
        var normalized = value.Replace('−', '-').Replace(',', '.').Replace("+", string.Empty, StringComparison.Ordinal).Trim();

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimal(string value)
    {
        var number = ParseDouble(value);
        return number is null ? null : (decimal)number.Value;
    }

    private static int? ParseDirection(string value) => ParseInt(value) switch
    {
        1 => 1,
        -1 => -1,
        _ => null,
    };

    private static bool? ParseBoolean(string value)
    {
        if (value.Length == 0)
        {
            return null;
        }

        var normalized = CatalogExcelSchema.NormalizeHeader(value);

        return normalized switch
        {
            "ha" or "true" or "1" or "yes" or "+" => true,
            "yo'q" or "false" or "0" or "no" or "-" => false,
            _ => null,
        };
    }

    /// <summary>`docs/03` §6.1 — superadmin anketasida faqat `Likert5`/`Likert7`.</summary>
    private static string? NormalizeQuestionType(string value)
    {
        var normalized = CatalogExcelSchema.NormalizeHeader(value);

        return normalized switch
        {
            "likert5" => "Likert5",
            "likert7" => "Likert7",
            _ => null,
        };
    }
}

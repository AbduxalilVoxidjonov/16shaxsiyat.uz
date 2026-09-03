using System.Globalization;
using ClosedXML.Excel;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Excel;

namespace StudentRoadMap.Infrastructure.Excel;

/// <summary>
/// Anketa `.xlsx` fayli — quruvchi qismi (o'quvchi/parser qismi:
/// `CatalogExcelWorkbook.Parse.cs`). `ClosedXML` (`docs/06`da ko'rsatilgan kutubxona,
/// `Infrastructure/Export/ExcelExporter` bilan bir xil paket — yangi bog'liqlik qo'shilmadi).
/// Holatsiz — `Singleton`.
///
/// <para>
/// Eksport va bo'sh shablon BITTA quruvchidan o'tadi, shu sabab shablon "boshqacha
/// ko'rinadigan" fayl emas: yuklab olingan namunani to'ldirib darhol qaytarib yuklash mumkin.
/// </para>
/// </summary>
internal sealed partial class CatalogExcelWorkbook : ICatalogExcelWorkbook
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E4ECF7");

    public byte[] Build(CatalogExcelTestDto test)
    {
        ArgumentNullException.ThrowIfNull(test);

        using var workbook = new XLWorkbook();

        WriteTestSheet(workbook, test);
        WriteScalesSheet(workbook, test);
        WriteBandsSheet(workbook, test);
        WriteQuestionsSheet(workbook, test);
        WriteInstructionsSheet(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteTestSheet(XLWorkbook workbook, CatalogExcelTestDto test)
    {
        var sheet = AddSheet(workbook, CatalogExcelSchema.TestSheet, CatalogExcelSchema.TestColumns);

        SetText(sheet.Cell(2, 1), test.Code);
        SetText(sheet.Cell(2, 2), test.NameUz);
        SetText(sheet.Cell(2, 3), test.DescriptionUz);
        sheet.Cell(2, 4).SetValue(test.EstimatedMinutes);
        sheet.Cell(2, 5).SetValue(test.PageSize);
        SetText(sheet.Cell(2, 6), test.ScoringMode);

        Finish(sheet);
    }

    private static void WriteScalesSheet(XLWorkbook workbook, CatalogExcelTestDto test)
    {
        var sheet = AddSheet(workbook, CatalogExcelSchema.ScalesSheet, CatalogExcelSchema.ScaleColumns);

        var row = 1;
        foreach (var scale in test.Scales)
        {
            row++;
            SetText(sheet.Cell(row, 1), scale.Code);
            SetText(sheet.Cell(row, 2), scale.NameUz);
            SetText(sheet.Cell(row, 3), scale.DescriptionUz);
        }

        Finish(sheet);
    }

    private static void WriteBandsSheet(XLWorkbook workbook, CatalogExcelTestDto test)
    {
        var sheet = AddSheet(workbook, CatalogExcelSchema.BandsSheet, CatalogExcelSchema.BandColumns);

        var row = 1;
        foreach (var scale in test.Scales)
        {
            foreach (var band in scale.InterpretationBands)
            {
                row++;
                SetText(sheet.Cell(row, 1), scale.Code);
                sheet.Cell(row, 2).SetValue(band.From);
                sheet.Cell(row, 3).SetValue(band.To);
                SetText(sheet.Cell(row, 4), band.Label);
            }
        }

        Finish(sheet);
    }

    private static void WriteQuestionsSheet(XLWorkbook workbook, CatalogExcelTestDto test)
    {
        var sheet = AddSheet(workbook, CatalogExcelSchema.QuestionsSheet, CatalogExcelSchema.QuestionColumns);

        var row = 1;
        foreach (var question in test.Questions)
        {
            row++;
            SetText(sheet.Cell(row, 1), question.Code);
            sheet.Cell(row, 2).SetValue(question.Order);
            SetText(sheet.Cell(row, 3), question.TextUz);
            SetText(sheet.Cell(row, 4), question.Scale);
            sheet.Cell(row, 5).SetValue(question.Direction);
            sheet.Cell(row, 6).SetValue((double)question.Weight);
            SetText(sheet.Cell(row, 7), question.IsRequired ? CatalogExcelSchema.YesLabel : CatalogExcelSchema.NoLabel);
            SetText(sheet.Cell(row, 8), question.Type);
        }

        Finish(sheet);
    }

    /// <summary>
    /// Ko'rsatma varag'i. Talqin oraliqlari qoidasi ATAYLAB batafsil (`docs/03` §6.3): usiz
    /// superadmin faylni muvaffaqiyatli import qilib bo'lgach NASHRDA to'siqqa uriladi va
    /// sababini tushunmaydi — xato paydo bo'lgan joydan ancha uzoqda ko'rinadi.
    /// </summary>
    private static void WriteInstructionsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add(CatalogExcelSchema.InstructionsSheet);

        var lines = new (string Text, bool IsHeading)[]
        {
            ("Anketani Excel orqali yuklash — qo'llanma", true),
            ("", false),
            ("Faylda beshta varaq bo'ladi. Varaq nomlarini va sarlavha qatorini (1-qator) o'zgartirmang —", false),
            ("ustunlar aynan shu nomlar bo'yicha topiladi. Ustunlar tartibini o'zgartirsangiz ham ishlaydi.", false),
            ("", false),
            ($"1) \"{CatalogExcelSchema.TestSheet}\" varag'i — bitta qator, anketaning o'zi", true),
            ($"{CatalogExcelSchema.TestCode} — lotin katta harflari, raqam, '-' va '_' (masalan STRESS-1). Takrorlanmasligi kerak.", false),
            ($"{CatalogExcelSchema.TestName} — o'quvchi ko'radigan nom.", false),
            ($"{CatalogExcelSchema.TestDescription} — ixtiyoriy qisqa izoh.", false),
            ($"{CatalogExcelSchema.TestMinutes} — testni yechish uchun taxminiy vaqt (butun son, daqiqa).", false),
            ($"{CatalogExcelSchema.TestPageSize} — bitta ekranda nechta savol ko'rsatilishi (butun son).", false),
            ($"{CatalogExcelSchema.TestScoringMode} — \"Scored\" (ball hisoblanadi va daraja chiqadi) yoki \"Survey\" (faqat javoblar yig'iladi, ball yo'q).", false),
            ("", false),
            ($"2) \"{CatalogExcelSchema.ScalesSheet}\" varag'i — o'lchanadigan o'qlar", true),
            ($"{CatalogExcelSchema.ScaleCode} — qisqa kod (masalan STRESS). Savollar shu kod orqali shkalaga bog'lanadi.", false),
            ($"{CatalogExcelSchema.ScaleName} — shkalaning o'zbekcha nomi.", false),
            ($"{CatalogExcelSchema.ScaleDescription} — ixtiyoriy izoh.", false),
            ("Har bir shkalada kamida 4 ta savol bo'lishi shart — kamrog'ida ball ishonchsiz bo'ladi.", false),
            ("", false),
            ($"3) \"{CatalogExcelSchema.BandsSheet}\" varag'i — natija darajalari (talqin oraliqlari)", true),
            ($"{CatalogExcelSchema.ScaleCode} — qaysi shkalaga tegishli (\"{CatalogExcelSchema.ScalesSheet}\" varag'idagi kod bilan bir xil).", false),
            ($"{CatalogExcelSchema.BandFrom} / {CatalogExcelSchema.BandTo} — foizdagi chegaralar. {CatalogExcelSchema.BandTo} INKLYUZIV (o'zi ham shu oraliqqa kiradi).", false),
            ($"{CatalogExcelSchema.BandLabel} — daraja nomi (masalan Past / O'rtacha / Yuqori).", false),
            ("", false),
            ("ORALIQ QOIDASI (buzilsa anketani NASHR QILIB BO'LMAYDI):", true),
            ("  - chegaralar faqat BUTUN SON bo'lsin (33 ha, 33.5 yo'q);", false),
            ("  - birinchi oraliq 0 dan boshlansin, oxirgisi 100 da tugasin;", false),
            ("  - oraliqlar orasida bo'shliq bo'lmasin va ustma-ust tushmasin:", false),
            ("    keyingi oraliqning \"Dan\" qiymati oldingisining \"Gacha\" qiymatidan aynan 1 ta katta bo'lsin.", false),
            ("", false),
            ("  To'g'ri:  0–33 Past · 34–66 O'rtacha · 67–100 Yuqori", false),
            ("  Xato:     0–33.3 / 33.4–66.6 (kasr son)", false),
            ("  Xato:     0–33 / 35–100 (bo'shliq: 34 hech qaysi oraliqqa tushmaydi)", false),
            ("  Xato:     0–50 / 50–100 (ustma-ust: 50 ikkala oraliqda)", false),
            ("  Xato:     0–99 (to'liq emas: 100 gacha yetmadi)", false),
            ("", false),
            ($"4) \"{CatalogExcelSchema.QuestionsSheet}\" varag'i — savollar", true),
            ($"{CatalogExcelSchema.QuestionCode} — savolning unikal kodi (masalan ST-Q01).", false),
            ($"{CatalogExcelSchema.QuestionOrder} — ko'rsatish tartibi (1 dan boshlanadigan butun son, takrorlanmasin).", false),
            ($"{CatalogExcelSchema.QuestionText} — o'quvchi o'qiydigan matn.", false),
            ($"{CatalogExcelSchema.QuestionScale} — shkala kodi (\"{CatalogExcelSchema.ScalesSheet}\" varag'idan).", false),
            ($"{CatalogExcelSchema.QuestionDirection} — 1 yoki -1.", false),
            ("    1  = to'g'ri savol: \"Qo'shilaman\" degan javob shkala ballini OSHIRADI.", false),
            ("    -1 = teskari savol: \"Qo'shilaman\" degan javob shkala ballini KAMAYTIRADI", false),
            ("         (ball hisoblashda javob qiymati teskari o'giriladi).", false),
            ($"{CatalogExcelSchema.QuestionWeight} — savolning og'irligi, odatda 1. Musbat son bo'lishi shart.", false),
            ($"{CatalogExcelSchema.QuestionRequired} — \"{CatalogExcelSchema.YesLabel}\" yoki \"{CatalogExcelSchema.NoLabel}\".", false),
            ($"{CatalogExcelSchema.QuestionType} — ixtiyoriy ustun: \"Likert5\" (5 ballik, standart) yoki \"Likert7\". Bo'sh qoldirilsa Likert5 olinadi.", false),
            ("", false),
            ("5) Cheklovlar", true),
            ($"  - fayl formati faqat .xlsx (Excel Workbook). Eski .xls va makrolı .xlsm qabul qilinmaydi;", false),
            ($"  - fayl hajmi {CatalogExcelLimits.MaxFileBytes / (1024 * 1024)} MB dan oshmasin;", false),
            ($"  - bitta varaqda {CatalogExcelLimits.MaxRows} tadan ortiq qator bo'lmasin;", false),
            ("  - kataklardagi formulalar hisoblanmaydi — Excel saqlagan oxirgi qiymat o'qiladi.", false),
            ("", false),
            ("Eng oson yo'l: mavjud anketalardan birini \"Excel'ga chiqarish\" bilan yuklab oling —", false),
            ("to'ldirilgan haqiqiy namuna chiqadi, uni nusxalab o'zingiznikini yozing.", false),
        };

        var row = 0;
        foreach (var (text, isHeading) in lines)
        {
            row++;
            var cell = sheet.Cell(row, 1);
            SetText(cell, text);
            if (isHeading)
            {
                cell.Style.Font.SetBold();
            }
        }

        sheet.Column(1).Width = 110;
        sheet.Column(1).Style.Alignment.SetWrapText(false);
    }

    private static IXLWorksheet AddSheet(XLWorkbook workbook, string name, IReadOnlyList<string> columns)
    {
        var sheet = workbook.Worksheets.Add(name);

        for (var i = 0; i < columns.Count; i++)
        {
            SetText(sheet.Cell(1, i + 1), columns[i]);
        }

        sheet.Row(1).Style.Font.SetBold();
        sheet.Row(1).Style.Fill.SetBackgroundColor(HeaderFill);
        sheet.SheetView.FreezeRows(1);

        return sheet;
    }

    private static void Finish(IXLWorksheet sheet) => sheet.Columns().AdjustToContents(minWidth: 10, maxWidth: 80);

    /// <summary>
    /// Matn katagi — HAR DOIM `Text` (`"@"`) formatida: aks holda Excel `ST-Q01` yoki `-1`
    /// ko'rinishidagi qiymatni "taxmin qilib" sana/formulaga aylantirib yuborishi mumkin
    /// (`ExcelExporter` dagi bilan bir xil sabab).
    /// </summary>
    private static void SetText(IXLCell cell, string? value)
    {
        cell.Style.NumberFormat.SetFormat("@");
        cell.SetValue(value ?? string.Empty);
    }

}

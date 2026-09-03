using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Excel;
using StudentRoadMap.Infrastructure.Excel;

namespace StudentRoadMap.Infrastructure.Tests.Excel;

/// <summary>
/// `CatalogExcelWorkbook` — anketa `.xlsx` faylini QURISH va O'QISH (P39).
///
/// <para>
/// Eng qimmatli sinov — <b>eksport → import aylanmasi</b>: fayl qurilib, DARHOL qayta
/// o'qiladi va natija boshlang'ich model bilan solishtiriladi. Yozuvchi bilan o'quvchi
/// bir-biridan uzilib ketsa (ustun qo'shildi, sarlavha o'zgardi, tartib almashdi) shu test
/// darhol qizaradi — aks holda nomuvofiqlik faqat superadmin haqiqiy fayl bilan urinib
/// ko'rgandagina, ishlab chiqarishda ko'rinardi.
/// </para>
/// </summary>
public sealed class CatalogExcelWorkbookTests
{
    private readonly CatalogExcelWorkbook _workbook = new();

    /// <summary>Ikkita shkala (oraliqlari bilan), teskari savol, kasr og'irlik, `Likert7` va `Majburiy = yo'q` — barcha maydon turi qamrab olinadi.</summary>
    private static CatalogExcelTestDto SampleTest() => new(
        Code: "STRESS-1",
        NameUz: "Stressga chidamlilik anketasi",
        DescriptionUz: "Namunaviy tavsif",
        EstimatedMinutes: 7,
        PageSize: 8,
        ScoringMode: "Scored",
        Scales:
        [
            new CatalogExcelScaleDto("STRESS", "Stressga munosabat", "Qisqa izoh",
            [
                new InterpretationBandDto(0, 33, "Past"),
                new InterpretationBandDto(34, 66, "O'rtacha"),
                new InterpretationBandDto(67, 100, "Yuqori"),
            ]),
            new CatalogExcelScaleDto("SUPPORT", "Ijtimoiy qo'llab-quvvatlanish", null,
            [
                new InterpretationBandDto(0, 50, "Past"),
                new InterpretationBandDto(51, 100, "Yuqori"),
            ]),
        ],
        Questions:
        [
            new CatalogExcelQuestionDto("ST-Q01", 1, "Kutilmagan vaziyatda xotirjam qolaman.", "Likert5", "STRESS", 1, 1.0m, true),
            new CatalogExcelQuestionDto("ST-Q02", 2, "Kichik muammo ham kayfiyatimni buzadi.", "Likert5", "STRESS", -1, 1.5m, true),
            new CatalogExcelQuestionDto("ST-Q03", 3, "Qiyinchilikni yechim deb qabul qilaman.", "Likert7", "STRESS", 1, 2.5m, false),
            new CatalogExcelQuestionDto("SU-Q01", 4, "Yaqinlarimdan yordam so'ray olaman.", "Likert5", "SUPPORT", 1, 1.0m, true),
        ]);

    // --- 1. Aylanma (eng muhim sinov) --------------------------------------------------------

    [Fact]
    public void QurishVaOqish_Aylanma_BoshlangichModelniAynanQaytaradi()
    {
        var original = SampleTest();

        var outcome = _workbook.Parse(_workbook.Build(original));

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Issues.Should().BeEmpty();
        outcome.Data.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void QurishVaOqish_Shablon_AylanmadaOzgarmaydi()
    {
        // Bo'sh shablon ham AYNAN shu quruvchidan o'tadi — ya'ni uni yuklab olib, to'ldirmasdan
        // qaytarib yuklash ham ishlaydi. Shablon "boshqacha ko'rinadigan" fayl emas.
        var outcome = _workbook.Parse(_workbook.Build(CatalogExcelTemplate.Sample));

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Data.Should().BeEquivalentTo(CatalogExcelTemplate.Sample);
    }

    [Fact]
    public void Oqish_UstunlarTartibiOzgarganda_BaribirOqiydi()
    {
        // Ustun TARTIBI ahamiyatsiz — moslashtirish sarlavha NOMI bo'yicha ketadi.
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        var sheet = book.Worksheet(CatalogExcelSchema.QuestionsSheet);
        sheet.Column(1).InsertColumnsBefore(1);
        sheet.Cell(1, 1).SetValue("Izoh");

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Data!.Questions.Should().BeEquivalentTo(SampleTest().Questions);
    }

    [Fact]
    public void Oqish_JavobTuriUstuniYoq_Likert5DebOqiydi()
    {
        // Egasining ustun ro'yxatida "Javob turi" yo'q — usiz yozilgan fayl ham ishlashi kerak.
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        var sheet = book.Worksheet(CatalogExcelSchema.QuestionsSheet);
        sheet.Column(8).Delete();

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Data!.Questions.Should().OnlyContain(q => q.Type == "Likert5");
    }

    // --- 2. Buzuq / noto'g'ri fayllar (500 emas, tushunarli xato) -----------------------------

    [Fact]
    public void Oqish_ZipBolmaganFayl_TushunarliXatoBeradi()
    {
        var outcome = _workbook.Parse(Encoding.UTF8.GetBytes("Bu oddiy matn, Excel emas."));

        outcome.Data.Should().BeNull();
        outcome.FileErrorMessage.Should().Contain(".xlsx");
    }

    [Fact]
    public void Oqish_BuzuqZip_TushunarliXatoBeradi()
    {
        // To'g'ri sehrli baytlar, lekin ichida yaroqli arxiv yo'q.
        var corrupt = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };

        var outcome = _workbook.Parse(corrupt);

        outcome.Data.Should().BeNull();
        outcome.FileErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Oqish_BoshFayl_TushunarliXatoBeradi()
    {
        _workbook.Parse([]).FileErrorMessage.Should().Be("Fayl bo'sh.");
    }

    [Fact]
    public void Oqish_EskiXlsFormat_AlohidaXabarBeradi()
    {
        // OLE2 (`.xls`) sehrli baytlari — foydalanuvchi nima qilishi kerakligini aniq bilishi uchun
        // umumiy "buzuq fayl" emas, alohida xabar.
        var xls = new byte[64];
        byte[] magic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        magic.CopyTo(xls, 0);

        var outcome = _workbook.Parse(xls);

        outcome.FileErrorMessage.Should().Contain(".xls");
        outcome.FileErrorMessage.Should().Contain("Excel Workbook (.xlsx)");
    }

    [Fact]
    public void Oqish_MakroliFayl_RadEtiladi()
    {
        var outcome = _workbook.Parse(WithExtraEntry(_workbook.Build(SampleTest()), "xl/vbaProject.bin", [1, 2, 3]));

        outcome.Data.Should().BeNull();
        outcome.FileErrorMessage.Should().Contain(".xlsm");
    }

    [Fact]
    public void Oqish_XatoXabarlariIchkiTafsilotSizdirmaydi()
    {
        // P31: xato matnida stack trace, kutubxona nomi yoki fayl yo'li BO'LMASLIGI kerak.
        string[] leaks = ["ClosedXML", "System.", "Exception", "at Studen", "\\", "/Users"];

        var messages = new[]
        {
            _workbook.Parse(Encoding.UTF8.GetBytes("matn")).FileErrorMessage,
            _workbook.Parse([0x50, 0x4B, 0x03, 0x04, 0x00]).FileErrorMessage,
            _workbook.Parse(WithExtraEntry(_workbook.Build(SampleTest()), "xl/vbaProject.bin", [1])).FileErrorMessage,
        };

        foreach (var message in messages)
        {
            message.Should().NotBeNullOrWhiteSpace();
            foreach (var leak in leaks)
            {
                message.Should().NotContain(leak);
            }
        }
    }

    // --- 3. Mazmun xatolari (fayl o'qiladi, lekin qatorlar noto'g'ri) -------------------------

    [Fact]
    public void Oqish_SavollarVaragiBosh_NoQuestionsXatosiBeradi()
    {
        var empty = SampleTest() with { Questions = [] };

        var outcome = _workbook.Parse(_workbook.Build(empty));

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Issues.Should().Contain(i => i.Code == "NO_QUESTIONS");
    }

    [Fact]
    public void Oqish_UstunYetishmasa_ColumnMissingXatosiBeradi()
    {
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        book.Worksheet(CatalogExcelSchema.QuestionsSheet).Column(4).Delete(); // "Shkala"

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Issues.Should().Contain(i =>
            i.Code == "COLUMN_MISSING" && i.Sheet == CatalogExcelSchema.QuestionsSheet);
    }

    [Fact]
    public void Oqish_VaraqYoq_SheetMissingXatosiBeradi()
    {
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        book.Worksheet(CatalogExcelSchema.QuestionsSheet).Delete();

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.Data.Should().BeNull();
        outcome.Issues.Should().Contain(i => i.Code == "SHEET_MISSING");
    }

    [Fact]
    public void Oqish_YonalishNotogri_QatorTashlanadiVaXatoBeriladi()
    {
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        book.Worksheet(CatalogExcelSchema.QuestionsSheet).Cell(2, 5).SetValue("ha");

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.Issues.Should().Contain(i => i.Code == "QUESTION_DIRECTION_INVALID");
        outcome.Data!.Questions.Should().HaveCount(3).And.NotContain(q => q.Code == "ST-Q01");
    }

    [Fact]
    public void Oqish_OraliqNomalumShkalaga_BandScaleUnknownBeradi()
    {
        var bytes = _workbook.Build(SampleTest());
        using var stream = new MemoryStream(bytes);
        using var book = new XLWorkbook(stream);
        book.Worksheet(CatalogExcelSchema.BandsSheet).Cell(2, 1).SetValue("YOQ-SHKALA");

        using var saved = new MemoryStream();
        book.SaveAs(saved);

        var outcome = _workbook.Parse(saved.ToArray());

        outcome.Issues.Should().Contain(i => i.Code == "BAND_SCALE_UNKNOWN");
    }

    // --- 4. Chegaralar -----------------------------------------------------------------------

    [Fact]
    public void Oqish_HajmChegarasidanKatta_RadEtiladi()
    {
        var big = new byte[CatalogExcelLimits.MaxFileBytes + 1];
        big[0] = 0x50;
        big[1] = 0x4B;
        big[2] = 0x03;
        big[3] = 0x04;

        _workbook.Parse(big).FileErrorMessage.Should().Contain("MB");
    }

    [Fact]
    public void Oqish_QatorChegarasidanKop_RadEtiladi()
    {
        var many = SampleTest() with
        {
            Questions = Enumerable.Range(1, CatalogExcelLimits.MaxRows + 1)
                .Select(i => new CatalogExcelQuestionDto($"Q{i:0000}", i, $"Savol {i}", "Likert5", "STRESS", 1, 1.0m, true))
                .ToList(),
        };

        var outcome = _workbook.Parse(_workbook.Build(many));

        outcome.Data.Should().BeNull();
        outcome.FileErrorMessage.Should().Contain(CatalogExcelLimits.MaxRows.ToString());
    }

    [Fact]
    public void Oqish_ChegaradagiQatorSoni_QabulQilinadi()
    {
        var atLimit = SampleTest() with
        {
            Questions = Enumerable.Range(1, CatalogExcelLimits.MaxRows)
                .Select(i => new CatalogExcelQuestionDto($"Q{i:0000}", i, $"Savol {i}", "Likert5", "STRESS", 1, 1.0m, true))
                .ToList(),
        };

        var outcome = _workbook.Parse(_workbook.Build(atLimit));

        outcome.FileErrorMessage.Should().BeNull();
        outcome.Data!.Questions.Should().HaveCount(CatalogExcelLimits.MaxRows);
    }

    /// <summary>Mavjud `.xlsx` arxiviga qo'shimcha yozuv qo'shadi (makro sinovlari uchun).</summary>
    private static byte[] WithExtraEntry(byte[] xlsx, string entryName, byte[] content)
    {
        using var stream = new MemoryStream();
        stream.Write(xlsx);
        stream.Position = 0;

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            using var entryStream = archive.CreateEntry(entryName).Open();
            entryStream.Write(content);
        }

        return stream.ToArray();
    }
}

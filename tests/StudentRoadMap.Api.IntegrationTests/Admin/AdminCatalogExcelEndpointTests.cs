using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Excel;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// Anketa Excel shabloni, eksporti va yuklash (P39, `docs/07` §3.4). Alohida
/// `IClassFixture` — rate limiter kvotasi (`CLAUDE.md` qoidasi, `AdminCatalogEndpointTests`
/// bilan bir xil sabab).
/// </summary>
public sealed class AdminCatalogExcelEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly PublicApiTestFactory _factory;

    public AdminCatalogExcelEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    /// <summary>Tizim metodikasi — eksport uchun "eng yaxshi namuna" (o'qish amali, BR-8 buzilmaydi).</summary>
    private static async Task<TestDefinition> CreateSystemTestAsync(AppDbContext db, string code)
    {
        var now = DateTimeOffset.UtcNow;
        var testId = Guid.NewGuid();
        var questions = Enumerable.Range(1, 4)
            .Select(i => Question.Create(Guid.NewGuid(), testId, $"{code}-Q{i}", i, $"Tizim savoli {i}", QuestionType.Likert5, "EI", i % 2 == 0 ? -1 : 1, 1.0m, isSystem: true))
            .ToList();

        var test = TestDefinition.CreateSystemPublished(
            testId, code, $"{code} nomi", null, displayOrder: 1, estimatedMinutes: 5, shuffleQuestions: false, pageSize: 10,
            scoringStrategyCode: "MBTI16", questions: questions, now: now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(ExcelContentType);
        content.Add(file, "file", fileName);
        return content;
    }

    // --- Shablon va eksport -------------------------------------------------------------------

    [Fact]
    public async Task ImportTemplate_XlsxFaylQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-template-admin");

        var response = await client.GetAsync("/api/admin/catalog/import-template.xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ExcelContentType);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportTest_TizimMetodikasi_XlsxQaytaradi()
    {
        Guid testId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            testId = (await CreateSystemTestAsync(db, "XLSXSYS")).Id;
        }

        using var client = await AuthenticatedClientAsync("catalog-excel-export-admin");

        var response = await client.GetAsync($"/api/admin/catalog/tests/{testId}/export.xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ExcelContentType);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportTest_NomavjudId_404Beradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-404-admin");

        var response = await client.GetAsync($"/api/admin/catalog/tests/{Guid.NewGuid()}/export.xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Aylanma: eksport → yuklash -----------------------------------------------------------

    /// <summary>
    /// EKSPORT → IMPORT aylanmasi HTTP darajasida: anketa API orqali quriladi, `export.xlsx`
    /// bilan yuklab olinadi va O'SHA fayl `parse-excel` ga qaytariladi. Natijadagi savollar,
    /// shkalalar va talqin oraliqlari boshlang'ich bilan bir xil bo'lishi kerak — ikki tomon
    /// ajralib ketsa shu test darhol ko'rsatadi.
    /// </summary>
    [Fact]
    public async Task Aylanma_EksportQilinganFaylQaytaOqilganda_BirXilChiqadi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-roundtrip-admin");

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "XLRT-1", nameUz = "Aylanma anketasi", descriptionUz = "Tavsif", estimatedMinutes = 9, pageSize = 7, shuffleQuestions = false, displayOrder = 3, scoringMode = "Scored" },
            TestJson.Options);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options))!;

        var scaleResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{created.Id}/scales",
            new
            {
                code = "STRESS",
                nameUz = "Stressga munosabat",
                descriptionUz = "Qisqa izoh",
                displayOrder = 0,
                interpretationBands = new[]
                {
                    new { from = 0d, to = 33d, label = "Past" },
                    new { from = 34d, to = 66d, label = "O'rtacha" },
                    new { from = 67d, to = 100d, label = "Yuqori" },
                },
            },
            TestJson.Options);
        scaleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var questions = Enumerable.Range(1, 4)
            .Select(i => new { code = $"RT-Q{i}", order = i, textUz = $"Savol {i}", type = "Likert5", scale = "STRESS", direction = i % 2 == 0 ? -1 : 1, weight = 1.0m, isRequired = true })
            .ToList();

        var importResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{created.Id}/questions/import",
            new { questions },
            TestJson.Options);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exported = await client.GetByteArrayAsync($"/api/admin/catalog/tests/{created.Id}/export.xlsx");

        using var content = FileContent(exported, "anketa-XLRT-1.xlsx");
        var parseResponse = await client.PostAsync("/api/admin/catalog/import/parse-excel", content);

        parseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var parsed = (await parseResponse.Content.ReadFromJsonAsync<ParseCatalogExcelResultDto>(TestJson.Options))!;

        parsed.Issues.Should().BeEmpty();
        parsed.Data.Should().NotBeNull();
        parsed.Data!.Code.Should().Be("XLRT-1");
        parsed.Data.NameUz.Should().Be("Aylanma anketasi");
        parsed.Data.EstimatedMinutes.Should().Be(9);
        parsed.Data.PageSize.Should().Be(7);
        parsed.Data.ScoringMode.Should().Be("Scored");

        parsed.Data.Questions.Should().BeEquivalentTo(
            questions.Select(q => new CatalogExcelQuestionDto(q.code, q.order, q.textUz, q.type, q.scale, q.direction, q.weight, q.isRequired)));

        parsed.Data.Scales.Should().BeEquivalentTo(new[]
        {
            new CatalogExcelScaleDto("STRESS", "Stressga munosabat", "Qisqa izoh",
            [
                new InterpretationBandDto(0, 33, "Past"),
                new InterpretationBandDto(34, 66, "O'rtacha"),
                new InterpretationBandDto(67, 100, "Yuqori"),
            ]),
        });
    }

    [Fact]
    public async Task ParseExcel_Shablon_OqiladiVaSaqlanmaydi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-parse-template-admin");

        var template = await client.GetByteArrayAsync("/api/admin/catalog/import-template.xlsx");

        using var content = FileContent(template, "shablon.xlsx");
        var response = await client.PostAsync("/api/admin/catalog/import/parse-excel", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var parsed = (await response.Content.ReadFromJsonAsync<ParseCatalogExcelResultDto>(TestJson.Options))!;
        parsed.Data!.Code.Should().Be("NAMUNA");

        // Hech narsa SAQLANMAYDI — katalogda "NAMUNA" kodli anketa paydo bo'lmasligi kerak.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.TestDefinitions.Should().NotContain(t => t.Code == "NAMUNA");
    }

    // --- Xavfsizlik ---------------------------------------------------------------------------

    [Fact]
    public async Task ParseExcel_ZipBolmaganFayl_400VaIchkiTafsilotsiz()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-bad-file-admin");

        using var content = FileContent("Bu Excel emas, oddiy matn."u8.ToArray(), "yolgon.xlsx");
        var response = await client.PostAsync("/api/admin/catalog/import/parse-excel", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IMPORT_FILE_INVALID");
        // P31: xato javobida stack trace, kutubxona nomi yoki fayl yo'li sizmasligi kerak.
        body.Should().NotContain("ClosedXML");
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("System.IO");
    }

    [Fact]
    public async Task ParseExcel_MakroliFayl_RadEtiladi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-macro-admin");

        var template = await client.GetByteArrayAsync("/api/admin/catalog/import-template.xlsx");
        var withMacro = WithVbaProject(template);

        using var content = FileContent(withMacro, "makro.xlsm");
        var response = await client.PostAsync("/api/admin/catalog/import/parse-excel", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("IMPORT_FILE_INVALID");
    }

    [Fact]
    public async Task ParseExcel_JudaKattaFayl_413Beradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-excel-too-large-admin");

        var big = new byte[CatalogExcelLimits.MaxFileBytes + 1024];
        big[0] = 0x50;
        big[1] = 0x4B;
        big[2] = 0x03;
        big[3] = 0x04;

        using var content = FileContent(big, "katta.xlsx");
        var response = await client.PostAsync("/api/admin/catalog/import/parse-excel", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task ExcelEndpointlari_Autentifikatsiyasiz_401Beradi()
    {
        using var client = _factory.CreateClient();

        (await client.GetAsync("/api/admin/catalog/import-template.xlsx")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        using var content = FileContent([0x50, 0x4B, 0x03, 0x04], "x.xlsx");
        (await client.PostAsync("/api/admin/catalog/import/parse-excel", content)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    private static byte[] WithVbaProject(byte[] xlsx)
    {
        using var stream = new MemoryStream();
        stream.Write(xlsx);
        stream.Position = 0;

        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Update, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("xl/vbaProject.bin").Open();
            entry.Write([1, 2, 3]);
        }

        return stream.ToArray();
    }
}

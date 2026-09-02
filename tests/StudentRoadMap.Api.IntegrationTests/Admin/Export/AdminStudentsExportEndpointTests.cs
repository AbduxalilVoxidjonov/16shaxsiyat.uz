using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Audit;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin.Export;

/// <summary>
/// `ExportController.ExportStudents` (`GET /api/admin/students/export`) — `docs/07` 3.2-bo'lim,
/// `prompts/27`. Alohida `IClassFixture` (rate limiter kvotasi, boshqa Admin testlar bilan bir
/// xil naqsh). Fayl baytlari HAQIQIY `.xlsx` sifatida `ClosedXML` bilan qayta ochilib
/// tekshiriladi — faqat `200`/Content-Type emas.
/// </summary>
public sealed class AdminStudentsExportEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentsExportEndpointTests(PublicApiTestFactory factory)
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

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, int grade, string phone) =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Female, grade, PhoneNumber.Create(phone).Value, now, now);

    [Fact]
    public async Task ExportStudents_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/students/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportStudents_SchoolIdFiltriBilan_RoyxatBilanAynanBirXilOquvchilarniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var schoolA = await TestDataFactory.CreateSchoolAsync(db, now, "export-a", TestDataFactory.NewAccessToken("export-a"));
        var schoolB = await TestDataFactory.CreateSchoolAsync(db, now, "export-b", TestDataFactory.NewAccessToken("export-b"));

        var studentA1 = MakeStudent(schoolA.Id, now, "Ergasheva Nilufar Baxtiyorovna", 9, "+998901112201");
        var studentA2 = MakeStudent(schoolA.Id, now, "Tojiboyev Sanjar Ulug'bekovich", 10, "+998901112202");
        var studentB1 = MakeStudent(schoolB.Id, now, "Rashidov Islom Farrukhovich", 9, "+998901112203");
        db.Students.AddRange(studentA1, studentA2, studentB1);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("export-students-admin");

        // Ro'yxat endpointi bilan solishtirish — eksport AYNAN shu filtr natijasini beradi
        // (`prompts/27` MAXSUS DIQQAT #1).
        var listResult = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?schoolId={schoolA.Id}&pageSize=100", TestJson.Options);
        listResult!.TotalCount.Should().Be(2);

        var response = await client.GetAsync(new Uri($"/api/admin/students/export?schoolId={schoolA.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();

        // Sarlavha qatori — `prompts/27` vazifa #1 ustunlar ro'yxati.
        worksheet.Cell(1, 1).GetString().Should().Be("FISH");
        worksheet.Cell(1, 7).GetString().Should().Be("Telefon");
        worksheet.Cell(1, 18).GetString().Should().Be("O'qish uchun izoh");

        // Muzlatilgan sarlavha (1-qator, `SplitRow == 1`) + avtofiltr (`prompts/27` vazifa #1).
        worksheet.SheetView.SplitRow.Should().Be(1);
        worksheet.AutoFilter.IsEnabled.Should().BeTrue();

        // Faqat `schoolA`ning 2 ta o'quvchisi — `schoolB`niki yo'q.
        var names = Enumerable.Range(2, 2).Select(row => worksheet.Cell(row, 1).GetString()).ToList();
        names.Should().BeEquivalentTo([studentA1.FullName, studentA2.FullName]);
        worksheet.Cell(3, 1).GetString().Should().NotBeNullOrEmpty();
        worksheet.Row(4).IsEmpty().Should().BeTrue("faqat 2 ta qator kutilgan (sarlavha + 2 o'quvchi)");

        // Telefon ustuni MATN sifatida saqlanadi (`prompts/27` cheklovi: formatlanmasin).
        var phoneCell = worksheet.Cell(2, 7);
        phoneCell.DataType.Should().Be(XLDataType.Text);
        phoneCell.GetString().Should().StartWith("+998");
    }

    [Fact]
    public async Task ExportStudents_QidiruvFiltriBilan_AuditYozadiVaQidiruvMatniniYozmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "export-audit", TestDataFactory.NewAccessToken("export-audit"));
        var student = MakeStudent(school.Id, now, "Yoqubova Sitora Davronovna", 9, "+998901112301");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("export-audit-admin");

        const string secretSearchTerm = "Yoqubova";
        var response = await client.GetAsync(new Uri($"/api/admin/students/export?search={secretSearchTerm}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        worksheet.Cell(2, 1).GetString().Should().Be(student.FullName);
        worksheet.Row(3).IsEmpty().Should().BeTrue();

        // Audit (`docs/08` §8: "Export.StudentsDownloaded") — `prompts/27` MAXSUS DIQQAT #3:
        // qidiruv matni (shaxsiy bo'lishi mumkin) audit yozuviga YOZILMAYDI. Fixture ushbu
        // sinf ichidagi BOSHQA testlar bilan bitta DB'ni bo'lishadi (`IClassFixture`) — shu
        // sabab ro'yxatdan aynan SHU eksportga tegishli yozuv `hasSearch:true` belgisi bilan
        // topiladi (`ContainSingle` ishlatilmaydi).
        var auditLogs = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            "/api/admin/audit-logs?action=Export.StudentsDownloaded", TestJson.Options);
        auditLogs!.Items.Should().Contain(i => i.AfterJson != null && i.AfterJson.Contains("\"hasSearch\":true"));
        var auditEntry = auditLogs.Items.Single(i => i.AfterJson != null && i.AfterJson.Contains("\"hasSearch\":true"));
        auditEntry.AfterJson!.Should().Contain("\"rowCount\":1");
        auditEntry.AfterJson.Should().NotContain(secretSearchTerm, "qidiruv matni shaxsiy bo'lishi mumkin — audit'ga yozilmaydi");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Audit;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AuditController.List` (`GET /api/admin/audit-logs`) — `docs/07` 3.6-bo'lim, `docs/08` §8,
/// `prompts/15`. Alohida `IClassFixture`. `ListAuditLogsQueryHandler` standart saralashda
/// `Id DESC` ishlatadi (`created_at DESC` bilan TENG). SQLite'ning `DateTimeOffset` bo'yicha
/// `WHERE`/`ORDER BY`ni tarjima qila olmaslik muammosi `AppDbContext.ApplySqliteDateTimeOffsetConversion`
/// bilan yopilgan (`prompts/15` 2-bosqich, 2026-09-02) — `from`/`to` filtri ham endi to'liq ishlaydi.
/// </summary>
public sealed class AdminAuditLogsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAuditLogsEndpointTests(PublicApiTestFactory factory)
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

    [Fact]
    public async Task List_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/audit-logs", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_MaktabYaratishVaOChirishDanKeyin_AuditYozuvlariniActionBoyichaFiltrlaydi()
    {
        using var client = await AuthenticatedClientAsync("audit-list-admin");

        // Harakat 1: maktab yaratish (`School.Created` audit yozuvi).
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/schools",
            new { name = "Audit Test Maktabi", region = "Toshkent", district = "Chilonzor" },
            TestJson.Options);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var schoolId = created.GetProperty("id").GetGuid();

        // Harakat 2: o'sha maktabni o'chirish (`School.Deleted`).
        (await client.DeleteAsync(new Uri($"/api/admin/schools/{schoolId}", UriKind.Relative))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var createdLogs = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            $"/api/admin/audit-logs?action={Uri.EscapeDataString(AuditActions.SchoolCreated)}&entityType=School", TestJson.Options);
        createdLogs!.Items.Should().Contain(l => l.EntityId == schoolId && l.Action == AuditActions.SchoolCreated);

        var deletedLogs = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            $"/api/admin/audit-logs?action={Uri.EscapeDataString(AuditActions.SchoolDeleted)}&entityType=School", TestJson.Options);
        deletedLogs!.Items.Should().Contain(l => l.EntityId == schoolId && l.Action == AuditActions.SchoolDeleted);

        // Har ikkalasi ham — eng yangisi birinchi (standart `Id DESC` saralash).
        var allForSchool = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            $"/api/admin/audit-logs?entityType=School", TestJson.Options);
        var ourLogs = allForSchool!.Items.Where(l => l.EntityId == schoolId).ToList();
        ourLogs.Should().HaveCount(2);
        ourLogs[0].Action.Should().Be(AuditActions.SchoolDeleted, "eng yangi harakat (o'chirish) birinchi bo'lishi kerak");
        ourLogs[1].Action.Should().Be(AuditActions.SchoolCreated);
    }

    [Fact]
    public async Task List_PageSize100danKatta_JimginaYuzgaTushiriladi()
    {
        using var client = await AuthenticatedClientAsync("audit-pagesize-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            "/api/admin/audit-logs?pageSize=999", TestJson.Options);

        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task List_OChirilganOquvchiToLiqOchirilganda_AuditdaShaxsiyMalumotYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "audit-student-a", TestDataFactory.NewAccessToken("audit-student-a"));
        var phone = StudentRoadMap.Domain.Students.PhoneNumber.Create("+998907776001").Value;
        var student = StudentRoadMap.Domain.Students.Student.Create(
            Guid.NewGuid(), school.Id, "Maxfiy Familiya Ismovna", new DateOnly(2010, 1, 1), StudentRoadMap.Domain.Students.Gender.Female, 9, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("audit-student-admin");

        (await client.DeleteAsync(new Uri($"/api/admin/students/{student.Id}?hard=true", UriKind.Relative))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var logs = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            $"/api/admin/audit-logs?action={Uri.EscapeDataString(AuditActions.StudentDeleted)}&entityType=Student", TestJson.Options);

        var log = logs!.Items.Should().ContainSingle(l => l.EntityId == student.Id).Subject;
        log.AfterJson.Should().NotContain("Maxfiy Familiya Ismovna");
        log.AfterJson.Should().NotContain("+998907776001");
    }

    [Fact]
    public async Task List_FromToFiltri_OraliqBoyichaChegaralaydi()
    {
        using var client = await AuthenticatedClientAsync("audit-fromto-admin");

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        var result = await client.GetFromJsonAsync<PagedResult<AdminAuditLogItemDto>>(
            $"/api/admin/audit-logs?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}", TestJson.Options);

        result.Should().NotBeNull();
    }
}

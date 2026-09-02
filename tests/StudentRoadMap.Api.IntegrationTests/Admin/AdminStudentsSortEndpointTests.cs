using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `StudentsController.List` — `DateTimeOffset` ustunlar (`lastAssessmentAt` standart,
/// `createdAt`) bo'yicha DB-darajasidagi saralash (Postgres'da `ix_students_last_at`
/// indeksidan foydalanadi). Avval SQLite bu ustunlarda `ORDER BY`ni tarjima qila olmagani
/// uchun ikkalasi `Skip` qilingan edi — `AppDbContext.ApplySqliteDateTimeOffsetConversion`
/// (`DateTimeOffset` → `long` UTC tick, faqat SQLite'da) bilan yopildi (`prompts/15` 2-bosqich,
/// 2026-09-02): endi `NULLS LAST` semantikasi (`ApplyLastAssessmentAtSort`) ham SQLite ostida
/// to'g'ri tekshiriladi.
/// </summary>
public sealed class AdminStudentsSortEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentsSortEndpointTests(PublicApiTestFactory factory)
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

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, string phone) =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9, PhoneNumber.Create(phone).Value, now, now);

    [Fact]
    public async Task List_StandartSaralash_LastAssessmentAtBoyichaKamayishVaNullsLast()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-sort-default", TestDataFactory.NewAccessToken("students-sort-default"));

        var never = MakeStudent(school.Id, now, "Never Assessed Student", "+998901112201");

        var older = MakeStudent(school.Id, now, "Older Assessment Student", "+998901112202");
        older.UpdateSnapshot(null, null, null, null, null, needsAttention: false, lastAssessmentAt: now.AddDays(-5), completedAssessmentCount: 1, now: now);

        var newer = MakeStudent(school.Id, now, "Newer Assessment Student", "+998901112203");
        newer.UpdateSnapshot(null, null, null, null, null, needsAttention: false, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);

        db.Students.AddRange(never, older, newer);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-sort-default-admin");

        // `sort` berilmasa — standart `-lastAssessmentAt` (`ix_students_last_at` indeksiga mos,
        // `docs/05` "DESC NULLS LAST"): eng yangi faollik birinchi, hech qachon test
        // topshirmagan o'quvchi (`LastAssessmentAt = null`) OXIRIDA.
        var result = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?schoolId={school.Id}", TestJson.Options);

        result!.Items.Select(s => s.Id).Should().ContainInOrder(newer.Id, older.Id, never.Id);
    }

    [Fact]
    public async Task List_SortCreatedAt_DBDarajasidaTogriTartiblaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-sort-created", TestDataFactory.NewAccessToken("students-sort-created"));

        // Ikkalasi ALOHIDA `SaveChangesAsync` chaqiruvida — `AppDbContext.SaveChangesAsync`
        // `CreatedAt`ni HAR BIR chaqiruvda haqiqiy soat bo'yicha (`IDateTime.UtcNow`) qo'yadi;
        // bitta chaqiruvda ikkalasi bir xil `CreatedAt` olib qolardi (tartib aniqlanmas edi).
        var first = MakeStudent(school.Id, now, "First Created Student", "+998901112211");
        db.Students.Add(first);
        await db.SaveChangesAsync();

        var second = MakeStudent(school.Id, now, "Second Created Student", "+998901112212");
        db.Students.Add(second);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-sort-created-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?schoolId={school.Id}&sort=createdAt", TestJson.Options);

        result!.Items.Select(s => s.Id).Should().ContainInOrder(first.Id, second.Id);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.List` saralash — `ListAssessmentsQueryHandler`dagi izohga qarang.
/// Standart saralash (`-startedAt`, `DateTimeOffset`) va `reliabilityScore` (`double`) —
/// ikkalasi ham endi DB darajasida SQLite'da ham ishlaydi (`AppDbContext.ApplySqliteDateTimeOffsetConversion`,
/// `prompts/15` 2-bosqich). Alohida `IClassFixture` (rate limiter kvotasi).
/// </summary>
public sealed class AdminAssessmentsSortEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsSortEndpointTests(PublicApiTestFactory factory)
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
    public async Task List_StandartSaralash_StartedAtBoyichaKamayish()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-sort-default", TestDataFactory.NewAccessToken("assess-sort-default"));
        var student = MakeStudent(school.Id, now, "Saralash Talabasi", "+998907771201");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var older = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-sort-older-0123456789ab", "uz", now.AddDays(-2), now.AddDays(5), now);
        var newer = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-sort-newer-0123456789ab", "uz", now, now.AddDays(7), now);
        db.Assessments.AddRange(older, newer);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-sort-default-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            $"/api/admin/assessments?schoolId={school.Id}", TestJson.Options);

        result!.Items.Select(a => a.Id).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Fact]
    public async Task List_SortReliabilityScore_DBDarajasidaTogriTartiblaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-sort-reliability", TestDataFactory.NewAccessToken("assess-sort-reliability"));
        var student = MakeStudent(school.Id, now, "Ishonchlilik Talabasi", "+998907771202");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var low = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-sort-low-0123456789abcd", "uz", now, now.AddDays(7), now);
        low.SetReliability(30.0, ReliabilityFlag.Unreliable, now);
        var high = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-sort-high-0123456789abcd", "uz", now, now.AddDays(7), now);
        high.SetReliability(90.0, ReliabilityFlag.Reliable, now);
        db.Assessments.AddRange(low, high);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-sort-reliability-admin");

        var ascending = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            $"/api/admin/assessments?schoolId={school.Id}&sort=reliabilityScore", TestJson.Options);
        ascending!.Items.Select(a => a.Id).Should().ContainInOrder(low.Id, high.Id);

        var descending = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            $"/api/admin/assessments?schoolId={school.Id}&sort=-reliabilityScore", TestJson.Options);
        descending!.Items.Select(a => a.Id).Should().ContainInOrder(high.Id, low.Id);
    }
}

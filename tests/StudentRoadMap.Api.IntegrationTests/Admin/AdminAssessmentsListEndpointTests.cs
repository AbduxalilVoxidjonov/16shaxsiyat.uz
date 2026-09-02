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
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.List` — `docs/07` 3.3-bo'lim, `prompts/15`. Alohida `IClassFixture`.
/// Bu yerdagi testlar `&sort=reliabilityScore` bilan aniq maydonni tekshiradi (standart
/// `-startedAt` saralashning o'zi `AdminAssessmentsSortEndpointTests`da alohida hujjatlashtirilgan
/// — ikkalasi ham endi SQLite ostida to'liq ishlaydi, `AppDbContext.ApplySqliteDateTimeOffsetConversion`).
/// </summary>
public sealed class AdminAssessmentsListEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsListEndpointTests(PublicApiTestFactory factory)
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

    private static Assessment MakeAssessment(Student student, School school, DateTimeOffset now, string sessionToken, Guid programId) =>
        Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now, expiresAt: now.AddDays(7), now: now);

    [Fact]
    public async Task List_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/assessments", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_SchoolIdVaStatusFiltri_ToGriNatijaQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var schoolA = await TestDataFactory.CreateSchoolAsync(db, now, "assess-list-a", TestDataFactory.NewAccessToken("assess-list-a"));
        var schoolB = await TestDataFactory.CreateSchoolAsync(db, now, "assess-list-b", TestDataFactory.NewAccessToken("assess-list-b"));

        var studentA = MakeStudent(schoolA.Id, now, "Rustamova Nilufar Odilovna", "+998907771101");
        var studentB = MakeStudent(schoolB.Id, now, "Sodiqov Jamshid Farhodovich", "+998907771102");
        db.Students.AddRange(studentA, studentB);
        await db.SaveChangesAsync();

        var listProgramId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var draftInSchoolA = MakeAssessment(studentA, schoolA, now, "assess-list-draft-a-0123456789ab", listProgramId);
        var draftInSchoolB = MakeAssessment(studentB, schoolB, now, "assess-list-draft-b-0123456789ab", listProgramId);
        db.Assessments.AddRange(draftInSchoolA, draftInSchoolB);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-list-admin");

        var bySchool = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            $"/api/admin/assessments?schoolId={schoolA.Id}&sort=reliabilityScore", TestJson.Options);
        bySchool!.Items.Should().ContainSingle(a => a.Id == draftInSchoolA.Id);
        bySchool.Items.Should().NotContain(a => a.Id == draftInSchoolB.Id);
        bySchool.Items.Single().StudentName.Should().Be(studentA.FullName);
        bySchool.Items.Single().SchoolName.Should().Be(schoolA.Name);

        var byStatus = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?status=Draft&sort=reliabilityScore", TestJson.Options);
        byStatus!.Items.Should().Contain(a => a.Id == draftInSchoolA.Id);
        byStatus.Items.Should().Contain(a => a.Id == draftInSchoolB.Id);
    }

    [Fact]
    public async Task List_OChirilganSessiya_RoYxatdaKoRinmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-list-deleted", TestDataFactory.NewAccessToken("assess-list-deleted"));
        var student = MakeStudent(school.Id, now, "Ochirilgan Sessiya Talabasi", "+998907771103");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var deletedProgramId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = MakeAssessment(student, school, now, "assess-list-tobe-deleted-0123456789", deletedProgramId);
        assessment.MarkDeleted(now);
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-list-deleted-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            $"/api/admin/assessments?schoolId={school.Id}&sort=reliabilityScore", TestJson.Options);

        result!.Items.Should().NotContain(a => a.Id == assessment.Id);
    }

    [Fact]
    public async Task List_PageSize100danKatta_JimginaYuzgaTushiriladi()
    {
        using var client = await AuthenticatedClientAsync("assessments-pagesize-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?pageSize=999&sort=reliabilityScore", TestJson.Options);

        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }
}

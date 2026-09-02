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
/// `StudentsController.List` — `docs/07` 3.2-bo'lim, `prompts/14` MAXSUS DIQQAT #1/#2.
/// Alohida `IClassFixture` (rate limiter kvotasi).
/// </summary>
public sealed class AdminStudentsListEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentsListEndpointTests(PublicApiTestFactory factory)
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

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, int grade = 9, string phone = "+998901234567") =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, grade, PhoneNumber.Create(phone).Value, now, now);

    [Fact]
    public async Task List_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/students", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_SchoolIdVaGradeFiltri_ToGriNatijaQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var schoolA = await TestDataFactory.CreateSchoolAsync(db, now, "students-list-a", TestDataFactory.NewAccessToken("students-list-a"));
        var schoolB = await TestDataFactory.CreateSchoolAsync(db, now, "students-list-b", TestDataFactory.NewAccessToken("students-list-b"));

        var studentA9 = MakeStudent(schoolA.Id, now, "Aliyev Sardor Bekzodovich", grade: 9, phone: "+998901111111");
        var studentA10 = MakeStudent(schoolA.Id, now, "Vositova Malika Rustamovna", grade: 10, phone: "+998901111112");
        var studentB9 = MakeStudent(schoolB.Id, now, "Karimov Jasur Odilovich", grade: 9, phone: "+998901111113");
        db.Students.AddRange(studentA9, studentA10, studentB9);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-list-admin");

        var bySchool = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?schoolId={schoolA.Id}&sort=fullName", TestJson.Options);
        bySchool!.Items.Should().HaveCount(2).And.OnlyContain(s => s.SchoolName == schoolA.Name);

        var bySchoolAndGrade = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?schoolId={schoolA.Id}&grade=9&sort=fullName", TestJson.Options);
        bySchoolAndGrade!.Items.Should().ContainSingle(s => s.Id == studentA9.Id);

        var bySearch = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?search=Karimov&sort=fullName", TestJson.Options);
        bySearch!.Items.Should().ContainSingle(s => s.Id == studentB9.Id);
    }

    [Fact]
    public async Task List_NeedsAttentionFiltri_FaqatMosOquvchilarniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-attention-a", TestDataFactory.NewAccessToken("students-attention-a"));

        var needsAttentionStudent = MakeStudent(school.Id, now, "Yusupova Dilnoza Anvarovna", phone: "+998901111121");
        needsAttentionStudent.UpdateSnapshot(null, null, 20.0, ActivityLevel.Passive, null, needsAttention: true, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);

        var okStudent = MakeStudent(school.Id, now, "Nazarov Shahzod Baxtiyorovich", phone: "+998901111122");
        okStudent.UpdateSnapshot("INTJ", 70.0, 80.0, ActivityLevel.Active, "IRA", needsAttention: false, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);

        db.Students.AddRange(needsAttentionStudent, okStudent);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-attention-admin");

        var attentionOnly = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?needsAttention=true&sort=fullName", TestJson.Options);
        attentionOnly!.Items.Should().ContainSingle(s => s.Id == needsAttentionStudent.Id);
        attentionOnly.Items.Should().NotContain(s => s.Id == okStudent.Id);

        var byPersonality = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?personalityType=INTJ&sort=fullName", TestJson.Options);
        byPersonality!.Items.Should().ContainSingle(s => s.Id == okStudent.Id);

        var byActivity = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?activityLevel=Passive&sort=fullName", TestJson.Options);
        byActivity!.Items.Should().ContainSingle(s => s.Id == needsAttentionStudent.Id);
    }

    [Fact]
    public async Task List_PageSize100danKatta_JimginaYuzgaTushiriladi()
    {
        using var client = await AuthenticatedClientAsync("students-pagesize-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?pageSize=999&sort=fullName", TestJson.Options);

        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.GetById` — `docs/07` 3.3-bo'lim: "yuqoridagi `latestAssessment`
/// shakli" (`AdminLatestAssessmentDto`, `Admin.Students`da mavjud — bu yerda YANGI DTO YO'Q,
/// `prompts/15` "AVVAL O'QI": javob shakllari aynan). Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsGetByIdEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsGetByIdEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetById_MavjudEmas_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("assessments-getbyid-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_YakunlanganSessiya_NatijaVaScaleMaydoniYoqliginiQaytaradi()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<StudentRoadMap.Infrastructure.Persistence.Seeding.DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-getbyid-a", TestDataFactory.NewAccessToken("assess-getbyid-a"));
        var mbtiTest = await TestDataFactory.CreatePublishedTestAsync(db, now, "GBA-MBTI", 1, questionCount: 1);

        var phone = PhoneNumber.Create("+998907772001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Nortoyev Aziz Shukurovich", new DateOnly(2009, 6, 12), Gender.Male, 8, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "assess-getbyid-session-token-0123456789ab";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-30), expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, mbtiTest.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == mbtiTest.Id)).Id;
        assessment.StartTest(mbtiTest.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
        assessment.CompleteTest(mbtiTest.Id, [questionId], now);
        assessment.Complete(now);
        assessment.SetReliability(85.5, ReliabilityFlag.Reliable, now);

        var normalizedScores = """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""";
        var levels = """{"EI":"I","SN":"N","TF":"T","JP":"J"}""";
        var testResult = TestResult.Create(
            Guid.NewGuid(), assessmentTest.Id, assessment.Id, "MBTI16", "{}", normalizedScores,
            scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "INTJ", levelsJson: levels, flagsJson: "[]");

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-getbyid-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminLatestAssessmentDto>(TestJson.Options))!;
        body.Id.Should().Be(assessment.Id);
        body.Results.Mbti16.Should().NotBeNull();
        body.Results.Mbti16!.ResultCode.Should().Be("INTJ");
        body.Results.Mbti16.TypeName.Should().Be("Loyihachi");
        body.Results.Big5.Should().BeNull();
        body.AiAnalysis.Should().BeNull("P16-P18 (AI modul) hali ulanmagan");
        body.AiHistory.Should().BeEmpty();

        // `CLAUDE.md` 9-band: `scale`/`scaleDirection` javobda HECH QACHON bo'lmaydi.
        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        raw.Should().NotContain("scaleDirection");
        JsonDocument.Parse(raw);
    }
}

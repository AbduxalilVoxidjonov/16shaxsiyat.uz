using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AdminLatestAssessmentDto.Tests`/`HasPersonalityBattery` — egasi topgan kamchilik
/// (2026-09-12): admin profili so'rovnoma-only sessiyada shaxsiyat batareyasi bloklarini
/// "Hali natija yo'q" deb ko'rsatardi, chunki `results`dagi `null` "test yo'q" va "test bor,
/// hali hisoblanmagan"ni ajrata olmasdi. `docs/07` 3.2-bo'lim.
/// </summary>
public sealed class AdminStudentLatestAssessmentTestsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentLatestAssessmentTestsEndpointTests(PublicApiTestFactory factory)
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

    private static async Task<Student> CreateStudentAsync(AppDbContext db, DateTimeOffset now, string seed)
    {
        var school = await TestDataFactory.CreateSchoolAsync(db, now, seed, TestDataFactory.NewAccessToken(seed));
        var phone = PhoneNumber.Create("+998901234567").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Test O'quvchi", new DateOnly(2009, 6, 12), Gender.Male, 8, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    private static async Task<Assessment> CreateDraftAssessmentAsync(AppDbContext db, DateTimeOffset now, Student student, string tokenSeed)
    {
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var sessionToken = TestDataFactory.NewAccessToken(tokenSeed);
        return Assessment.Create(
            Guid.NewGuid(), student.Id, student.SchoolId, sessionToken, "uz", programId,
            startedAt: now.AddMinutes(-30), expiresAt: now.AddDays(7), now: now);
    }

    /// <summary>Berilgan testni to'liq yakunlaydi (`InProgress ──▶ Completed`) — `Status: "Completed"` uchun.</summary>
    private static async Task CompleteAssessmentTestAsync(AppDbContext db, Assessment assessment, AssessmentTest assessmentTest, TestDefinition testDefinition, DateTimeOffset now)
    {
        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == testDefinition.Id)
            .Select(q => q.Id)
            .ToListAsync();

        assessment.StartTest(testDefinition.Id, now);
        foreach (var questionId in questionIds)
        {
            assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
        }

        assessment.CompleteTest(testDefinition.Id, questionIds, now);
    }

    [Fact]
    public async Task GetById_SorovnomaOnlySessiya_BittaSurveyElement_HasPersonalityBatteryFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var student = await CreateStudentAsync(db, now, "latest-tests-survey-only");
        var surveyTest = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "INTELLECT-SURVEY", displayOrder: 1, questionCount: 2,
            scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);

        var assessment = await CreateDraftAssessmentAsync(db, now, student, "latest-tests-survey-only");
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, surveyTest.Id, displayOrder: 1, totalCount: 2);
        assessment.AddTest(assessmentTest);
        await CompleteAssessmentTestAsync(db, assessment, assessmentTest, surveyTest, now);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("latest-tests-survey-only-admin");
        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.LatestAssessment.Should().NotBeNull();
        body.LatestAssessment!.Tests.Should().ContainSingle();
        body.LatestAssessment.Tests[0].Code.Should().Be("INTELLECT-SURVEY");
        body.LatestAssessment.Tests[0].ScoringMode.Should().Be("Survey");
        body.LatestAssessment.Tests[0].Status.Should().Be("Completed");
        body.LatestAssessment.Tests[0].BatteryRole.Should().Be("None",
            "`Survey` bloki batareyaga kirmaydi (`PersonalityBattery.RoleOf`)");
        body.LatestAssessment.HasPersonalityBattery.Should().BeFalse(
            "so'rovnoma-only sessiyada ilmiy shaxsiyat batareyasi umuman yo'q");

        // Batareya bo'lmagani uchun `results` bloklari ham bo'sh — mijoz buni "test yo'q" deb
        // o'qishi kerak, `tests`dagi ro'yxat orqali.
        body.LatestAssessment.Results.Mbti16.Should().BeNull();
        body.LatestAssessment.Results.Big5.Should().BeNull();
        body.LatestAssessment.Results.Riasec.Should().BeNull();
        body.LatestAssessment.Results.Activity.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ShaxsiyatBatareyaliSessiya_ToRttaScoredElement_HasPersonalityBatteryTrue_TartibDisplayOrderBoyicha()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var student = await CreateStudentAsync(db, now, "latest-tests-battery");

        var testA = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BAT-A", displayOrder: 1, questionCount: 1);
        var testB = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BAT-B", displayOrder: 2, questionCount: 1);
        var testC = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BAT-C", displayOrder: 3, questionCount: 1);
        var testD = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BAT-D", displayOrder: 4, questionCount: 1);

        var assessment = await CreateDraftAssessmentAsync(db, now, student, "latest-tests-battery");

        // Ataylab `AssessmentTest.DisplayOrder` `TestDefinition.DisplayOrder`dan FARQLI —
        // tartib manbai `AssessmentTest.DisplayOrder` ekanini isbotlash uchun (B=1, D=2, C=3, A=4).
        var assessmentTestB = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testB.Id, displayOrder: 1, totalCount: 1);
        var assessmentTestD = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testD.Id, displayOrder: 2, totalCount: 1);
        var assessmentTestC = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testC.Id, displayOrder: 3, totalCount: 1);
        var assessmentTestA = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testA.Id, displayOrder: 4, totalCount: 1);

        assessment.AddTest(assessmentTestB);
        assessment.AddTest(assessmentTestD);
        assessment.AddTest(assessmentTestC);
        assessment.AddTest(assessmentTestA);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("latest-tests-battery-admin");
        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.LatestAssessment.Should().NotBeNull();
        body.LatestAssessment!.Tests.Should().HaveCount(4);
        body.LatestAssessment.Tests.Select(t => t.Code).Should().ContainInOrder("BAT-B", "BAT-D", "BAT-C", "BAT-A");
        body.LatestAssessment.Tests.Should().OnlyContain(t => t.ScoringMode == "Scored");
        body.LatestAssessment.HasPersonalityBattery.Should().BeTrue(
            "hammasi `Standard` + `Scored` — `PersonalityBattery.Includes` mezoni");
    }

    [Fact]
    public async Task GetById_AralashSessiya_ScoredVaSurveyIkkalasiHamRoyxatda_HasPersonalityBatteryTrue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var student = await CreateStudentAsync(db, now, "latest-tests-mixed");

        var batteryTest = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BAT-MIX", displayOrder: 1, questionCount: 1);
        var surveyTest = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "SURVEY-MIX", displayOrder: 2, questionCount: 1,
            scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);

        var assessment = await CreateDraftAssessmentAsync(db, now, student, "latest-tests-mixed");
        var batteryAssessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, batteryTest.Id, displayOrder: 1, totalCount: 1);
        var surveyAssessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, surveyTest.Id, displayOrder: 2, totalCount: 1);
        assessment.AddTest(batteryAssessmentTest);
        assessment.AddTest(surveyAssessmentTest);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("latest-tests-mixed-admin");
        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.LatestAssessment.Should().NotBeNull();
        body.LatestAssessment!.Tests.Should().HaveCount(2);
        body.LatestAssessment.Tests.Should().Contain(t => t.Code == "BAT-MIX" && t.ScoringMode == "Scored");
        body.LatestAssessment.Tests.Should().Contain(t => t.Code == "SURVEY-MIX" && t.ScoringMode == "Survey");
        body.LatestAssessment.HasPersonalityBattery.Should().BeTrue(
            "kamida bitta `Standard`+`Scored` bloki bor — `Survey` bloki bu bayroqqa ta'sir qilmaydi");
    }

    /// <summary>
    /// `BatteryRole` (code-review, 2026-09-14) — `PersonalityBattery.RoleOf` DOMEN qoidasidan
    /// (`ScoringStrategyCode` bo'yicha), metodika KODIDAN emas: `MBTI16` strategiyasi
    /// `PersonalityType` rolini beradi, `Survey` bloki esa `None`.
    /// </summary>
    [Fact]
    public async Task GetById_MBTI16StrategiyaliTest_BatteryRolePersonalityType_SurveyBlokiNone()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var student = await CreateStudentAsync(db, now, "latest-tests-role");

        var typeTest = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "ROLE-TYPE", displayOrder: 1, questionCount: 1, scoringStrategyCode: "MBTI16");
        var surveyTest = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "ROLE-SURVEY", displayOrder: 2, questionCount: 1,
            scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);

        var assessment = await CreateDraftAssessmentAsync(db, now, student, "latest-tests-role");
        var typeAssessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, typeTest.Id, displayOrder: 1, totalCount: 1);
        var surveyAssessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, surveyTest.Id, displayOrder: 2, totalCount: 1);
        assessment.AddTest(typeAssessmentTest);
        assessment.AddTest(surveyAssessmentTest);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("latest-tests-role-admin");
        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.LatestAssessment.Should().NotBeNull();
        body.LatestAssessment!.Tests.Should().Contain(t => t.Code == "ROLE-TYPE" && t.BatteryRole == "PersonalityType");
        body.LatestAssessment.Tests.Should().Contain(t => t.Code == "ROLE-SURVEY" && t.BatteryRole == "None");
    }

    [Fact]
    public async Task GetById_SessiyaYoqOquvchi_LatestAssessmentNull()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var student = await CreateStudentAsync(db, now, "latest-tests-no-session");

        using var client = await AuthenticatedClientAsync("latest-tests-no-session-admin");
        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.Assessments.Should().BeEmpty();
        body.LatestAssessment.Should().BeNull();
    }
}

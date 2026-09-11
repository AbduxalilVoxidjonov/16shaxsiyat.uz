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
/// `StudentsController.GetById` — `docs/07` 3.2-bo'lim (individual profil). Alohida `IClassFixture`.
/// </summary>
public sealed class AdminStudentGetByIdEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentGetByIdEndpointTests(PublicApiTestFactory factory)
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
        using var client = await AuthenticatedClientAsync("students-getbyid-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/students/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_YakunlanganSessiyaBilan_ToLiqProfilQaytaradi_VaScaleMaydoniYoq()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            // `TypeCatalog` — `typeName` ("Loyihachi") uchun kerak (`DbSeeder`, P05-P08).
            var seeder = seedScope.ServiceProvider.GetRequiredService<StudentRoadMap.Infrastructure.Persistence.Seeding.DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-getbyid-a", TestDataFactory.NewAccessToken("students-getbyid-a"));
        // ⚠️ HAQIQIY batareya anketasi (`Standard` + `Scored` + `MBTI16` STRATEGIYASI), lekin
        // kodi ATAYLAB `MBTI16` EMAS — `GBI-MBTI`. `results.MBTI16` bloki 2026-09-03 dan buyon
        // metodika KODI emas, batareya ROLI bo'yicha to'ldiriladi (`StudentProfileMapping`).
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "GBI-MBTI", 1, questionCount: 1, scoringStrategyCode: "MBTI16");

        var phone = PhoneNumber.Create("+998901234567").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Nortoyeva Kamola Shukurovna", new DateOnly(2009, 6, 12), Gender.Female, 8, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "admin-getbyid-test-session-token-0123456789";
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
            Guid.NewGuid(), assessmentTest.Id, assessment.Id, mbtiTest.Code, "{}", normalizedScores,
            scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "INTJ", levelsJson: levels, flagsJson: "[]");

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-getbyid-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.Student.Id.Should().Be(student.Id);
        body.Student.School.Id.Should().Be(school.Id);
        body.Assessments.Should().ContainSingle(a => a.Id == assessment.Id && a.IsLatest);
        body.LatestAssessment.Should().NotBeNull();
        body.LatestAssessment!.Results.Mbti16.Should().NotBeNull();
        body.LatestAssessment.Results.Mbti16!.ResultCode.Should().Be("INTJ");
        body.LatestAssessment.Results.Mbti16.TypeName.Should().Be("Loyihachi");
        body.LatestAssessment.Results.Mbti16.Axes["EI"].Letter.Should().Be("I");
        body.LatestAssessment.Results.Big5.Should().BeNull();
        body.LatestAssessment.AiAnalysis.Should().BeNull("P16-P18 (AI modul) hali ulanmagan");
        body.LatestAssessment.AiHistory.Should().BeEmpty();

        // `CLAUDE.md` 9-band: `scale`/`scaleDirection` o'quvchi profilida HECH QACHON bo'lmaydi.
        var raw = await client.GetStringAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        raw.Should().NotContain("scaleDirection");
        using var rawDocument = JsonDocument.Parse(raw); // yaroqli JSON ekanligini tasdiqlaydi.

        // `docs/07` 3.2 "KALIT NOMLARI": `results` kalitlari — `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`
        // (`AdminTestResultsDto` dagi `JsonPropertyName` bilan qulflangan). Anketa KODI boshqacha
        // (`GBI-MBTI`) bo'lishi mumkin — kalit batareya ROLIDAN kelib chiqadi.
        // `ReadFromJsonAsync<AdminStudentProfileDto>` kalitni o'zi qayta bog'laydi va bu farqni
        // KO'RMAYDI — shu sabab xom JSON ustidan tekshiriladi (2026-09-03 tekshiruvi).
        var rawResults = rawDocument.RootElement
            .GetProperty("latestAssessment")
            .GetProperty("results");
        rawResults.TryGetProperty("MBTI16", out _).Should().BeTrue(
            "shartnoma kaliti — `MBTI16` (anketa KODI `GBI-MBTI` bo'lsa ham: kalit rolga bog'liq, kodga emas)");
        rawResults.TryGetProperty("mbti16", out _).Should().BeFalse("camelCase kalit shartnomani buzadi");
    }

    /// <summary>P52 (2026-09-11): anonim o'quvchi profili `birthDate`/`age`/`phone` `null` bilan YIQILMASDAN qaytadi.</summary>
    [Fact]
    public async Task GetById_AnonimOquvchi_BirthDateVaPhoneNullBilanQaytadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-anon-getbyid", TestDataFactory.NewAccessToken("students-anon-getbyid"));
        var anonymousStudent = Student.CreateAnonymous(Guid.NewGuid(), school.Id, now, now);
        db.Students.Add(anonymousStudent);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-anon-getbyid-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/students/{anonymousStudent.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<AdminStudentProfileDto>(TestJson.Options))!;
        body.Student.BirthDate.Should().BeNull();
        body.Student.Age.Should().BeNull();
        body.Student.Phone.Should().BeNull();
    }
}

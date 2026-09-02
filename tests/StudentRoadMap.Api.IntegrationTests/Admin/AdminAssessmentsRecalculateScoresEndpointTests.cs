using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.RecalculateScores` — `docs/07` 3.3-bo'lim, `prompts/15` "⚠️ ENG
/// MUHIM" va MAXSUS DIQQAT #6 (idempotentlik). Sozlash — HAQIQIY ommaviy sessiya oqimi
/// (`StartSession → StartTest → SaveAnswers → CompleteTest → CompleteSession`) orqali, RIASEC
/// shaklidagi anketa bilan (`InterpretationBands`siz haqiqiy scoring, `TestDataFactory`
/// izohiga qarang — `SUM` bo'lmagani uchun bandssiz ishlaydi). Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsRecalculateScoresEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsRecalculateScoresEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedAdminClientAsync(string username)
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
    public async Task RecalculateScores_MavjudEmasSessiya_404Qaytaradi()
    {
        using var client = await AuthenticatedAdminClientAsync("assessments-recalc-404-admin");

        var response = await client.PostAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}/recalculate-scores", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecalculateScores_HaliHechQandayNatijaYoq_400ValidationErrorQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-recalc-draft", TestDataFactory.NewAccessToken("assess-recalc-draft"));
        var phone = PhoneNumber.Create("+998907774001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Qoraboyev Sanjar Rustamovich", new DateOnly(2010, 1, 1), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        // `Draft` holatida — hech qanday test yakunlanmagan, `TestResult` yo'q.
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-recalc-draft-session-01234", "uz", now, now.AddDays(7), now);
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedAdminClientAsync("assessments-recalc-draft-admin");

        var response = await client.PostAsync(new Uri($"/api/admin/assessments/{assessment.Id}/recalculate-scores", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecalculateScores_YakunlanganSessiya_IdempotentVaAuditYoziladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("recalc-ok");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-recalc-ok", accessToken);
        var testDefinition = await TestDataFactory.CreatePublishedRiasecShapedTestWithOptionalExtrasAsync(db, now, "RIASEC", 1, extraOptionalCount: 0);

        // Haqiqiy ommaviy oqim orqali sessiyani to'liq yakunlaymiz (real `ScoringEngine` +
        // `ReliabilityCalculator` — hand-crafted `TestResult` EMAS).
        using var publicClient = _factory.CreateClient();
        var startCommand = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Nabiyev Otabek Alisherovich", new DateOnly(2010, 5, 5), Gender.Male, 9, "A",
            "+998907774002", null, null, true, "uz");
        var startResponse = await publicClient.PostAsJsonAsync("/api/public/sessions", startCommand, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        publicClient.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await publicClient.PostAsync(new Uri("/api/public/sessions/tests/RIASEC/start", UriKind.Relative), content: null);

        var questionIds = testDefinition.Questions.OrderBy(q => q.DisplayOrder).Select(q => q.Id).ToList();
        var answersPayload = new { answers = questionIds.Select(id => new { questionId = id, value = 3, durationMs = 1000 }).ToList() };
        (await publicClient.PostAsJsonAsync("/api/public/sessions/tests/RIASEC/answers", answersPayload, TestJson.Options)).EnsureSuccessStatusCode();

        (await publicClient.PostAsync(new Uri("/api/public/sessions/tests/RIASEC/complete", UriKind.Relative), content: null)).EnsureSuccessStatusCode();
        (await publicClient.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null)).EnsureSuccessStatusCode();

        Guid assessmentId;
        double? originalReliability;
        string? originalResultCode;
        using (var lookupScope = _factory.Services.CreateScope())
        {
            var lookupDb = lookupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assessment = await lookupDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
            assessmentId = assessment.Id;
            originalReliability = assessment.ReliabilityScore;

            var testResult = await lookupDb.TestResults.AsNoTracking().SingleAsync(r => r.AssessmentId == assessmentId);
            originalResultCode = testResult.ResultCode;
        }

        using var adminClient = await AuthenticatedAdminClientAsync("assessments-recalc-ok-admin");

        var firstResponse = await adminClient.PostAsync(new Uri($"/api/admin/assessments/{assessmentId}/recalculate-scores", UriKind.Relative), content: null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = (await firstResponse.Content.ReadFromJsonAsync<AdminRecalculateScoresResultDto>(TestJson.Options))!;

        // Javoblar o'zgarmagan — qayta hisoblash BIR XIL natija berishi shart (determinizm, ADR-6).
        first.Results.Riasec.Should().NotBeNull();
        first.Results.Riasec!.ResultCode.Should().Be(originalResultCode);
        first.ReliabilityScore.Should().Be(originalReliability);
        first.Changed.Should().BeFalse("javoblar o'zgarmagan bo'lsa qayta hisoblash bir xil natija berishi shart");

        var secondResponse = await adminClient.PostAsync(new Uri($"/api/admin/assessments/{assessmentId}/recalculate-scores", UriKind.Relative), content: null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = (await secondResponse.Content.ReadFromJsonAsync<AdminRecalculateScoresResultDto>(TestJson.Options))!;

        // `prompts/15` MAXSUS DIQQAT #6: ikki marta chaqirilsa natija bir xil (idempotentlik).
        second.Results.Riasec!.ResultCode.Should().Be(first.Results.Riasec.ResultCode);
        second.Results.Riasec.Types.Should().BeEquivalentTo(first.Results.Riasec.Types);
        second.ReliabilityScore.Should().Be(first.ReliabilityScore);
        second.ReliabilityFlag.Should().Be(first.ReliabilityFlag);
        second.Changed.Should().BeFalse();

        // Audit — `Assessment.ScoresRecalculated`, shaxsiy ma'lumotsiz (`CLAUDE.md` 6-band).
        using (var auditScope = _factory.Services.CreateScope())
        {
            var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var auditLogs = await auditDb.AuditLogs.AsNoTracking()
                .Where(a => a.Action == AuditActions.AssessmentScoresRecalculated && a.EntityId == assessmentId)
                .ToListAsync();

            auditLogs.Should().HaveCount(2);
            foreach (var log in auditLogs)
            {
                log.BeforeJson.Should().NotContain("Nabiyev");
                log.AfterJson.Should().NotContain("Nabiyev");
                log.BeforeJson.Should().NotContain("+998907774002");
                log.AfterJson.Should().NotContain("+998907774002");
            }
        }
    }
}
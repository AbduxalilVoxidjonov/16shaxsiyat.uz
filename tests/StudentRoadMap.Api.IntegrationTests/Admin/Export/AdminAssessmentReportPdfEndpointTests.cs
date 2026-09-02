using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin.Export;

/// <summary>
/// `ExportController.AssessmentReportPdf` (`GET /api/admin/assessments/{id}/report.pdf`) —
/// `docs/07` 3.3-bo'lim, `prompts/27` vazifa #2. Alohida `IClassFixture`. Fayl baytlari HAQIQIY
/// PDF ekanini `%PDF-`/`%%EOF` sarlavha-oxiri belgilari orqali tekshiradi — faqat `200` emas.
/// </summary>
public sealed class AdminAssessmentReportPdfEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentReportPdfEndpointTests(PublicApiTestFactory factory)
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

    private static void AssertLooksLikeValidPdf(byte[] bytes)
    {
        bytes.Length.Should().BeGreaterThan(100);
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
        // `%%EOF` PDF fayl OXIRIDA (odatda so'nggi bir necha bayt ichida) bo'lishi shart —
        // haqiqiy, to'liq yozilgan (kesilmagan) faylni tasdiqlaydi.
        var tail = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
        tail.Should().Contain("%%EOF");
    }

    [Fact]
    public async Task ReportPdf_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}/report.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReportPdf_MavjudEmasSessiya_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("report-pdf-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}/report.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReportPdf_AiTahliliHaliYoq_ToGriPdfQaytaradiVaYiqilmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "report-pdf-noai", TestDataFactory.NewAccessToken("report-pdf-noai"));
        var mbtiTest = await TestDataFactory.CreatePublishedTestAsync(db, now, "PDF-MBTI", 1, questionCount: 1);

        var phone = PhoneNumber.Create("+998901112401").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Sultonova Madina G'ayratovna", new DateOnly(2009, 3, 5), Gender.Female, 8, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "report-pdf-noai-session-token-0123456789";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-20), expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, mbtiTest.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == mbtiTest.Id)).Id;
        assessment.StartTest(mbtiTest.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
        assessment.CompleteTest(mbtiTest.Id, [questionId], now);
        assessment.Complete(now);
        assessment.SetReliability(78.0, ReliabilityFlag.Reliable, now);

        var testResult = TestResult.Create(
            Guid.NewGuid(), assessmentTest.Id, assessment.Id, "MBTI16", "{}", """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""",
            scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "INTJ",
            levelsJson: """{"EI":"I","SN":"N","TF":"T","JP":"J"}""", flagsJson: "[]");

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("report-pdf-noai-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{assessment.Id}/report.pdf", UriKind.Relative));

        // `prompts/27` MAXSUS DIQQAT #4: AI hali yo'q ("AiAnalysis" bo'sh) bo'lsa ham PDF
        // TO'G'RI chiqadi — hujjat YIQILMAYDI (500 emas, 200 haqiqiy PDF bilan).
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        AssertLooksLikeValidPdf(bytes);
    }

    [Fact]
    public async Task ReportPdf_AiTahliliBorHolatda_ToGriPdfQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "report-pdf-ai", TestDataFactory.NewAccessToken("report-pdf-ai"));
        var phone = PhoneNumber.Create("+998901112402").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Nazarov Elyor Shuxratovich", new DateOnly(2008, 11, 20), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "report-pdf-ai-session-token-0123456789ab";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-25), expiresAt: now.AddDays(7), now: now);
        db.Assessments.Add(assessment);

        var aiAnalysis = AiAnalysis.Create(Guid.NewGuid(), assessment.Id, AiProvider.Gemini, "gemini-1.5-pro", "v1.0", now);
        aiAnalysis.Start();
        aiAnalysis.Succeed(
            responseJson: "{}",
            summary: "Umumiy xulosa matni.",
            personalityPortrait: "Shaxsiyat portreti matni.",
            now: now,
            strengthsJson: """["Tahliliy fikrlash","Mustaqillik"]""",
            growthAreasJson: """["Jamoaviy ish"]""",
            attentionFlagsJson: "[]");
        db.AiAnalyses.Add(aiAnalysis);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("report-pdf-ai-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{assessment.Id}/report.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        AssertLooksLikeValidPdf(bytes);
    }
}

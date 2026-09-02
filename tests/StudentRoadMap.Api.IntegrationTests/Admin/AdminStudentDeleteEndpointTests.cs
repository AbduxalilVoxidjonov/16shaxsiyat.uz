using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `StudentsController.Delete` — `docs/07` 3.2-bo'lim, `prompts/14` MAXSUS DIQQAT #5.
/// Alohida `IClassFixture`.
/// </summary>
public sealed class AdminStudentDeleteEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentDeleteEndpointTests(PublicApiTestFactory factory)
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

    /// <summary>To'liq zanjir bilan o'quvchi yaratadi: Student → Assessment → AssessmentTest → Answer → TestResult → AiAnalysis.</summary>
    private static async Task<(Domain.Schools.School School, Student Student, Assessment Assessment, Guid AssessmentTestId)> SeedFullChainAsync(
        AppDbContext db, DateTimeOffset now, string slugSeed, string fullName, string phone)
    {
        var school = await TestDataFactory.CreateSchoolAsync(db, now, slugSeed, TestDataFactory.NewAccessToken(slugSeed));
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, $"{slugSeed}-TEST".ToUpperInvariant(), 1, questionCount: 1);

        var student = Student.Create(Guid.NewGuid(), school.Id, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9, PhoneNumber.Create(phone).Value, now, now);
        db.Students.Add(student);

        var sessionToken = $"{slugSeed}-session-token-0123456789abcdef01234567";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-10), expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testDefinition.Id)).Id;
        assessment.StartTest(testDefinition.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 3, null, 1500, now);
        assessment.CompleteTest(testDefinition.Id, [questionId], now);
        assessment.Complete(now);

        var testResult = TestResult.Create(Guid.NewGuid(), assessmentTest.Id, assessment.Id, testDefinition.Code, "{}", "{}", scoringVersion: 1, testVersion: 1, computedAt: now);

        var aiAnalysis = Domain.Ai.AiAnalysis.Create(Guid.NewGuid(), assessment.Id, Domain.Ai.AiProvider.Gemini, "gemini-test", "v1.0", now);

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        db.AiAnalyses.Add(aiAnalysis);
        await db.SaveChangesAsync();

        return (school, student, assessment, assessmentTest.Id);
    }

    [Fact]
    public async Task Delete_MavjudEmas_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("students-delete-404-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/students/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_HardSizYumshoqOchirish_StudentBelgilanadiVaBogliqYozuvlarSaqlanibQoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var (_, student, assessment, _) = await SeedFullChainAsync(db, now, "students-delete-soft", "Ergasheva Nilufar Davronovna", "+998901234571");

        using var client = await AuthenticatedClientAsync("students-delete-soft-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Ro'yxatda/`GetById`da endi ko'rinmaydi (soft-delete query filter).
        var getAfterDelete = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Students.IgnoreQueryFilters().SingleAsync(s => s.Id == student.Id)).IsDeleted.Should().BeTrue();

        // Yumshoq o'chirishda bog'liq yozuvlar SAQLANIB QOLADI (faqat `hard=true`da o'chadi).
        (await verifyDb.Assessments.IgnoreQueryFilters().AnyAsync(a => a.Id == assessment.Id)).Should().BeTrue();
        (await verifyDb.TestResults.AnyAsync(r => r.AssessmentId == assessment.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_HardTrue_StudentVaBarchaBogliqYozuvlarniToLiqOchiradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var (_, student, assessment, assessmentTestId) = await SeedFullChainAsync(db, now, "students-delete-hard", "Tojiboyev Sanjar Ilhomovich", "+998901234572");

        var assessmentId = assessment.Id;

        using var client = await AuthenticatedClientAsync("students-delete-hard-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/students/{student.Id}?hard=true", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await verifyDb.Students.IgnoreQueryFilters().AnyAsync(s => s.Id == student.Id)).Should().BeFalse("student butunlay o'chirilishi kerak");
        (await verifyDb.Assessments.IgnoreQueryFilters().AnyAsync(a => a.Id == assessmentId)).Should().BeFalse("sessiya o'chirilishi kerak");
        (await verifyDb.AssessmentTests.AnyAsync(t => t.AssessmentId == assessmentId)).Should().BeFalse("test bloki o'chirilishi kerak");
        (await verifyDb.Answers.AnyAsync(a => a.AssessmentTestId == assessmentTestId)).Should().BeFalse("javoblar o'chirilishi kerak");
        (await verifyDb.TestResults.AnyAsync(r => r.AssessmentId == assessmentId)).Should().BeFalse("natijalar o'chirilishi kerak");
        (await verifyDb.AiAnalyses.AnyAsync(a => a.AssessmentId == assessmentId)).Should().BeFalse("AI tahlili o'chirilishi kerak");

        // Audit'da faqat `{studentId, deletedAt, adminId, hard}` — shaxsiy ma'lumot (ism, telefon) YO'Q.
        var auditLog = await verifyDb.AuditLogs.SingleAsync(a => a.Action == "Student.Deleted" && a.EntityId == student.Id);
        auditLog.AfterJson.Should().NotContain("Tojiboyev").And.NotContain("+998901234572");
    }
}

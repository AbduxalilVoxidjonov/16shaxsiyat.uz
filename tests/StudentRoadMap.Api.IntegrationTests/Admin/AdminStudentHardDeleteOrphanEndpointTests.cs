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
/// QA topilmasi (2026-09-02): `DeleteStudentCommandHandler.HardDeleteAsync` `IgnoreQueryFilters`
/// bilan avval SOFT o'chirilgan sessiyalarni ham topib tozalashi kerak (`prompts/14` MAXSUS
/// DIQQAT #5) — kod buni qo'llab-quvvatlaydi, lekin bu ALOHIDA tasdiqlanmagan edi
/// (`AdminStudentDeleteEndpointTests`dagi hard-delete testi faqat ODATDAGI, hali soft
/// o'chirilmagan sessiya bilan ishlaydi). Bu — yetim (orphan) yozuv qolib ketmasligini
/// qulflaydi: "o'chirildi" degani sessiya avval yumshoq o'chirilgan bo'lsa ham chindan
/// o'chirilgan bo'lishi kerak (maxfiylik/GDPR talabi). Alohida `IClassFixture`.
/// </summary>
public sealed class AdminStudentHardDeleteOrphanEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentHardDeleteOrphanEndpointTests(PublicApiTestFactory factory)
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
    public async Task Delete_HardTrue_AvvalSoftOchirilganSessiyaHamToLiqTozalanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-hard-orphan", TestDataFactory.NewAccessToken("students-hard-orphan"));
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, "HARD-ORPHAN-TEST", 1, questionCount: 1);

        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Rustamova Zarina Aziz qizi", new DateOnly(2010, 1, 1),
            Gender.Female, 9, PhoneNumber.Create("+998901234573").Value, now, now);
        db.Students.Add(student);

        const string sessionToken = "hard-orphan-session-token-0123456789abcdef012345";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-10), expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testDefinition.Id)).Id;
        assessment.StartTest(testDefinition.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 1800, now);
        assessment.CompleteTest(testDefinition.Id, [questionId], now);
        assessment.Complete(now);

        var testResult = TestResult.Create(Guid.NewGuid(), assessmentTest.Id, assessment.Id, testDefinition.Code, "{}", "{}", scoringVersion: 1, testVersion: 1, computedAt: now);

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        await db.SaveChangesAsync();

        var assessmentId = assessment.Id;
        var assessmentTestId = assessmentTest.Id;

        // Sessiyani ADMIN hard-delete'dan OLDIN yumshoq o'chiramiz — `docs/05` §2:
        // `assessments.is_deleted`. Global query filter (`AppDbContext.OnModelCreating`) uni
        // endi ODATDAGI so'rovlarda yashiradi, aynan shu holatni sinaymiz.
        assessment.MarkDeleted(now);
        await db.SaveChangesAsync();

        using var verifyBeforeScope = _factory.Services.CreateScope();
        var verifyBeforeDb = verifyBeforeScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyBeforeDb.Assessments.AnyAsync(a => a.Id == assessmentId)).Should().BeFalse("global query filter soft-o'chirilgan sessiyani yashiradi");
        (await verifyBeforeDb.Assessments.IgnoreQueryFilters().AnyAsync(a => a.Id == assessmentId)).Should().BeTrue("yozuv hali DB'da — faqat SOFT o'chirilgan");

        using var client = await AuthenticatedClientAsync("students-hard-orphan-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/students/{student.Id}?hard=true", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyAfterScope = _factory.Services.CreateScope();
        var verifyAfterDb = verifyAfterScope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await verifyAfterDb.Students.IgnoreQueryFilters().AnyAsync(s => s.Id == student.Id)).Should().BeFalse("student butunlay o'chirilishi kerak");

        // Yetim (orphan) yozuv QOLMASLIGI kerak — avval SOFT o'chirilgan bo'lsa ham.
        (await verifyAfterDb.Assessments.IgnoreQueryFilters().AnyAsync(a => a.Id == assessmentId))
            .Should().BeFalse("avval soft o'chirilgan sessiya hard-delete'da HAM tozalanishi kerak (IgnoreQueryFilters)");
        (await verifyAfterDb.AssessmentTests.AnyAsync(t => t.Id == assessmentTestId))
            .Should().BeFalse("test bloki yetim qolmasligi kerak");
        (await verifyAfterDb.Answers.AnyAsync(a => a.AssessmentTestId == assessmentTestId))
            .Should().BeFalse("javob yetim qolmasligi kerak");
        (await verifyAfterDb.TestResults.AnyAsync(r => r.AssessmentId == assessmentId))
            .Should().BeFalse("natija yetim qolmasligi kerak");
    }
}

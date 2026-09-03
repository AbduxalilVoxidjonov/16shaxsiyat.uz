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
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.GetAnswers` — `docs/07` 3.3-bo'lim: "Xom javoblar (audit uchun)",
/// `prompts/15` MAXSUS DIQQAT #5. Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsAnswersEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsAnswersEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetAnswers_MavjudEmasSessiya_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("assessments-answers-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}/answers", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAnswers_XomJavoblarniQaytaradi_TestCodeFiltriIshlaydiVaScaleYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-answers-a", TestDataFactory.NewAccessToken("assess-answers-a"));
        var testA = await TestDataFactory.CreatePublishedTestAsync(db, now, "ANS-A", 1, questionCount: 2);
        var testB = await TestDataFactory.CreatePublishedTestAsync(db, now, "ANS-B", 2, questionCount: 1);

        var phone = PhoneNumber.Create("+998907773001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Ergasheva Nodira Bahodirovna", new DateOnly(2009, 3, 3), Gender.Female, 9, phone, now, now);
        db.Students.Add(student);

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-answers-session-0123456789ab", "uz", programId, now.AddMinutes(-30), now.AddDays(7), now);
        var assessmentTestA = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testA.Id, 1, totalCount: 2);
        var assessmentTestB = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testB.Id, 2, totalCount: 1);
        assessment.AddTest(assessmentTestA);
        assessment.AddTest(assessmentTestB);

        var questionsA = await db.Questions.AsNoTracking().Where(q => q.TestDefinitionId == testA.Id).OrderBy(q => q.DisplayOrder).ToListAsync();
        var questionB = await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testB.Id);

        assessment.StartTest(testA.Id, now.AddMinutes(-30));
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[0].Id, 3, null, 1200, now.AddMinutes(-29));
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[1].Id, 5, null, 900, now.AddMinutes(-28));
        // Qayta javob — `revisionCount` oshishini tekshirish uchun.
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[1].Id, 4, null, 1500, now.AddMinutes(-27));

        assessment.StartTest(testB.Id, now.AddMinutes(-20));
        assessmentTestB.UpsertAnswer(Guid.NewGuid(), questionB.Id, 2, null, 800, now.AddMinutes(-19));

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-answers-admin");

        var all = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers", TestJson.Options);
        all!.Answers.Should().HaveCount(3);
        all.Answers.Select(a => a.TestCode).Should().Contain(["ANS-A", "ANS-B"]);

        var revisedAnswer = all.Answers.Single(a => a.QuestionId == questionsA[1].Id);
        revisedAnswer.RawValue.Should().Be(4);
        revisedAnswer.RevisionCount.Should().Be(1);
        revisedAnswer.DurationMs.Should().Be(1500);
        revisedAnswer.QuestionText.Should().NotBeNullOrWhiteSpace();

        var onlyB = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers?testCode=ANS-B", TestJson.Options);
        onlyB!.Answers.Should().ContainSingle(a => a.QuestionId == questionB.Id);

        // Signallar filtrga QARAMAY butun sessiya bo'yicha (`AdminAssessmentAnswersDto` izohi):
        // `ReliabilityCalculator` ham sessiya darajasida ishlaydi.
        onlyB.Session.AnsweredCount.Should().Be(3, "signallar `testCode` filtridan qat'i nazar BUTUN sessiya bo'yicha");

        var notAssigned = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers?testCode=NOPE", TestJson.Options);
        notAssigned!.Answers.Should().BeEmpty();

        // ⚠️ 2026-09-03 dan buyon `scale`/`scaleDirection`/`scaleNameUz`/`effectiveValue`
        // ADMIN javobida ATAYLAB BOR (egasining talabi: javobning ma'nosi ko'rinsin) —
        // `CLAUDE.md` 9-bandi faqat O'QUVCHI API'siga tegishli va u
        // `PublicTestQuestionsEndpointTests` da xom JSON + swagger sxemasi ustidan qulflangan.
        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}/answers", UriKind.Relative));
        raw.Should().Contain("\"scaleDirection\"");
        raw.Should().Contain("\"effectiveValue\"");
    }
}

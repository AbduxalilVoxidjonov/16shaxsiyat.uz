using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// P52 (`docs/18` §8) MAJBURIY regressiya: 4 ta tizim metodikasi (`MBTI16`/`BIG5`/`RIASEC`/
/// `ACTIVITY`, `IsSystem = true`, `ScoringMode = Scored`) uchun `GetTestQuestions` javobi va
/// scoring P52 (tarmoqlanuvchi so'rovnoma) o'zgarishlaridan KEYIN ham AYNAN bir xil xatti-harakat
/// ko'rsatishi kerak: bo'lim/shart qo'shilmagan, yangi maydonlar hammasi `null`.
///
/// Alohida `IClassFixture` — o'zining `PublicApiTestFactory` nusxasi (rate limiter boshqa
/// testlar bilan aralashmasligi uchun, mavjud naqsh — `PublicCompleteFlowEndpointTests`).
/// </summary>
public sealed class PublicSystemMethodologyRegressionTests : IClassFixture<PublicApiTestFactory>
{
    private static readonly string[] SystemTestCodes = ["MBTI16", "BIG5", "RIASEC", "ACTIVITY"];

    private readonly PublicApiTestFactory _factory;

    public PublicSystemMethodologyRegressionTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// `docs/18` B-1/B-2: tizim metodikalarida bo'lim/shart HECH QACHON bo'lmaydi (seed ularni
    /// qo'shmaydi) — shu sabab `sections` har doim `null` va har savolning P52 maydonlari
    /// (`sectionId`/`placeholder`/`inputPattern`/`maxLength`/`minSelections`/`maxSelections`/
    /// `visibility`/`currentText`/`currentValues`) hammasi `null`. `scale`/`scaleDirection`
    /// baribir yo'q (`PublicTestQuestionsEndpointTests`da allaqachon qulflangan).
    /// </summary>
    [Fact]
    public async Task GetTestQuestions_TortTizimMetodikasida_YangiMaydonlarNullVaSectionsNull()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("sysreg1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-sysreg1", accessToken);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Rahimova Zarina Ulug'bekovna", new DateOnly(2010, 2, 2));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        foreach (var testCode in SystemTestCodes)
        {
            var startResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null);
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' boshlanishi kerak");

            var testDefinitionId = await db.TestDefinitions.AsNoTracking().Where(t => t.Code == testCode).Select(t => t.Id).SingleAsync();
            var expectedActiveQuestionCount = await db.Questions.AsNoTracking()
                .CountAsync(q => q.TestDefinitionId == testDefinitionId && q.IsActive);

            var response = await client.GetAsync(new Uri($"/api/public/sessions/tests/{testCode}/questions?page=1", UriKind.Relative));
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' savollari o'qilishi kerak");

            var body = await response.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);
            body.Should().NotBeNull();

            body!.Sections.Should().BeNull($"'{testCode}' tizim metodikasida bo'lim yo'q (B-3) — sections har doim null bo'lishi kerak");
            body.TotalQuestions.Should().Be(expectedActiveQuestionCount, $"'{testCode}' uchun savollar soni P52dan OLDINGIDEK bazadagi faol savollarga teng bo'lishi kerak");

            foreach (var question in body.Questions)
            {
                question.SectionId.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida bo'lim bo'lmaydi");
                question.Placeholder.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida placeholder bo'lmaydi");
                question.InputPattern.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida inputPattern bo'lmaydi");
                question.MaxLength.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida maxLength bo'lmaydi");
                question.MinSelections.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida minSelections bo'lmaydi");
                question.MaxSelections.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida maxSelections bo'lmaydi");
                question.Visibility.Should().BeNull($"'{testCode}'/'{question.Code}': tizim savolida ko'rsatish sharti bo'lmaydi (B-2)");
                question.CurrentText.Should().BeNull($"'{testCode}'/'{question.Code}': hali javob berilmagan — currentText null bo'lishi kerak");
                question.CurrentValues.Should().BeNull($"'{testCode}'/'{question.Code}': hali javob berilmagan — currentValues null bo'lishi kerak");
            }

            // Keyingi test faqat oldingisi yakunlangach ochiladi (`TEST_NOT_UNLOCKED`, mavjud
            // xatti-harakat) — shu sabab davom etish uchun shu yerda to'ldirib yakunlaymiz.
            await AnswerAllAndCompleteAsync(client, db, testCode);
        }
    }

    /// <summary>Testning barcha faol savollariga `value=3` (haqiqiy bankdagi hammasi Likert5) javob berib yakunlaydi.</summary>
    private static async Task AnswerAllAndCompleteAsync(HttpClient client, AppDbContext db, string testCode)
    {
        var testDefinitionId = await db.TestDefinitions.AsNoTracking().Where(t => t.Code == testCode).Select(t => t.Id).SingleAsync();
        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == testDefinitionId && q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        foreach (var chunk in questionIds.Chunk(50))
        {
            var payload = new { answers = chunk.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
            var saveResponse = await client.PostAsJsonAsync($"/api/public/sessions/tests/{testCode}/answers", payload, TestJson.Options);
            saveResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' javoblari saqlanishi kerak");
        }

        var completeResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/complete", UriKind.Relative), content: null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' yakunlanishi kerak");
    }

    /// <summary>
    /// Scoring qatlami (`Domain/Scoring/*`) P52 doirasida O'ZGARTIRILMADI — bu test faqat
    /// P52 dan keyin ham har 4 metodika uchun `complete` haqiqiy `TestResult` yozishini va
    /// natija maydonlari (`resultCode`/normallashtirilgan ballar) bo'sh QOLMASLIGINI qulflaydi
    /// (chuqur formula regressiyasi allaqachon `PublicCompleteFlowEndpointTests`da — masalan
    /// BIG5 `CompositeIndex == 50.0` — qamrab olingan, u ham shu o'zgarishlardan keyin YASHIL).
    /// </summary>
    [Fact]
    public async Task CompleteTest_TortTizimMetodikasida_TestResultHarDoimgidekYoziladi()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("sysreg2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-sysreg2", accessToken);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Tojiboyev Sardor Baxtiyor o'g'li", new DateOnly(2010, 2, 12));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        foreach (var testCode in SystemTestCodes)
        {
            await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null);
            await AnswerAllAndCompleteAsync(client, db, testCode);
        }

        var assessmentId = await db.Assessments.AsNoTracking()
            .OrderByDescending(a => a.StartedAt).Select(a => a.Id).FirstAsync();

        var results = await db.TestResults.AsNoTracking().Where(r => r.AssessmentId == assessmentId).ToListAsync();
        results.Should().HaveCount(4, "har 4 tizim metodikasi uchun TestResult yozilishi kerak (Scored, Survey emas)");

        foreach (var result in results)
        {
            result.NormalizedScoresJson.Should().NotBeNullOrWhiteSpace($"'{result.TestCode}' uchun normallashtirilgan ballar bo'sh bo'lmasligi kerak");
            result.ScoringVersion.Should().BeGreaterThan(0);
        }
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", "+998909998877", null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }
}

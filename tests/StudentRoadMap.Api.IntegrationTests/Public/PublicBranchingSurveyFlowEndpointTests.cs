using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.SaveAnswers;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `docs/18` §8 — MAJBURIY integratsiya testi: to'liq tarmoqlanuvchi so'rovnoma oqimi.
/// Filtr savoli (`FILTER`) "Ha" (1) bo'lsa bog'liq savol (`FOLLOWUP`) ko'rinadi va javob
/// qabul qilinadi; filtr keyin "Yo'q" (2) ga o'zgartirilsa `FOLLOWUP` yashiriladi va
/// yakunlashda uning ESKI javobi bazadan o'chiriladi (`AssessmentTest.RemoveAnswers`,
/// `docs/18` §2.7/§4.3). Bitta `AssessmentTest` FAQAT bir marta `Completed`ga o'tishi mumkin
/// (domen holat mashinasi) — shu sabab javob almashtirish TAMOMLASHDAN OLDIN, bitta yakuniy
/// `complete` chaqirig'i bilan sinaladi (ikkinchi "qayta complete" — idempotentlik — allaqachon
/// `PublicCompleteFlowEndpointTests`da qamrab olingan).
/// </summary>
public sealed class PublicBranchingSurveyFlowEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicBranchingSurveyFlowEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    [Fact]
    public async Task ToliqOqim_FiltrHaBolganda_BoglikSavolGoringan_KeyinYoqgaOzgartirilsa_EskiJavobBazadanOchiriladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("branch1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-branch1", accessToken);
        var test = await TestDataFactory.CreatePublishedBranchingFilterSurveyAsync(db, now, "BRANCH1", 1);
        var filterQuestion = test.Questions.Single(q => q.Code == "BRANCH1-FILTER");
        var followUpQuestion = test.Questions.Single(q => q.Code == "BRANCH1-FOLLOWUP");

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Yusupova Kamola Bahtiyorovna", new DateOnly(2010, 3, 3));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/BRANCH1/start", UriKind.Relative), content: null);

        // 1) Filtr = "Ha" (1) — bog'liq savol ENDI ko'rinadi va javob qabul qilinadi.
        var filterYesRequest = new { answers = new[] { new { questionId = filterQuestion.Id, value = 1, durationMs = 400 } } };
        var filterYesResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/BRANCH1/answers", filterYesRequest, TestJson.Options);
        filterYesResponse.StatusCode.Should().Be(HttpStatusCode.OK, "filtr savoliga 'Ha' javobi qabul qilinishi kerak");

        var followUpRequest = new { answers = new[] { new { questionId = followUpQuestion.Id, text = "Matematika kursi", durationMs = 1200 } } };
        var followUpResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/BRANCH1/answers", followUpRequest, TestJson.Options);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK, "filtr 'Ha' bo'lgani uchun bog'liq savol KO'RINADI — QUESTION_NOT_VISIBLE bermasligi kerak");

        var afterFirstSave = await client.GetFromJsonAsync<GetTestQuestionsResult>("/api/public/sessions/tests/BRANCH1/questions?page=1", TestJson.Options);
        afterFirstSave!.Questions.Single(q => q.Id == followUpQuestion.Id).CurrentText.Should().Be("Matematika kursi");

        // 2) Filtrni "Yo'q" (2) ga o'zgartiramiz — bog'liq savol ENDI YASHIRILADI (eski javobi
        // bazada hali turibdi, hozircha o'chirilmagan — faqat `complete`da tozalanadi).
        var filterNoRequest = new { answers = new[] { new { questionId = filterQuestion.Id, value = 2, durationMs = 300 } } };
        var filterNoResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/BRANCH1/answers", filterNoRequest, TestJson.Options);
        filterNoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var followUpAnswerBeforeComplete = await db.Answers.AsNoTracking().FirstOrDefaultAsync(a => a.QuestionId == followUpQuestion.Id);
        followUpAnswerBeforeComplete.Should().NotBeNull("complete chaqirilmaguncha eski javob hali bazada turadi");

        // 3) Yakunlash: `FOLLOWUP` endi yashirilgan (majburiy bo'lsa ham) — bloklamaydi, VA
        // uning eski javobi bazadan o'chiriladi (`docs/18` §2.7/§4.3).
        var completeResponse = await client.PostAsync(new Uri("/api/public/sessions/tests/BRANCH1/complete", UriKind.Relative), content: null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK, "yashirilgan majburiy savol (FOLLOWUP) yakunlashni bloklamasligi kerak");
        var completeBody = await completeResponse.Content.ReadFromJsonAsync<CompleteTestResult>(TestJson.Options);
        completeBody!.Status.Should().Be("Completed");

        var followUpAnswerAfterComplete = await db.Answers.AsNoTracking().FirstOrDefaultAsync(a => a.QuestionId == followUpQuestion.Id);
        followUpAnswerAfterComplete.Should().BeNull("yashirilgan savolning javobi complete paytida BAZADAN o'chirilishi kerak");

        var filterAnswerAfterComplete = await db.Answers.AsNoTracking().SingleAsync(a => a.QuestionId == filterQuestion.Id);
        filterAnswerAfterComplete.RawValue.Should().Be(2, "filtr savolining O'ZI o'chirilmaydi, faqat qiymati yangilangan");
    }

    /// <summary>
    /// `docs/18` §4.2: filtr savoliga hali javob berilmasdan turib bog'liq savolga yozishga
    /// urinish `400 QUESTION_NOT_VISIBLE` bilan rad etiladi.
    /// </summary>
    [Fact]
    public async Task KorinmaydiganSavolgaYozish_QuestionNotVisible400Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("branch2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-branch2", accessToken);
        var test = await TestDataFactory.CreatePublishedBranchingFilterSurveyAsync(db, now, "BRANCH2", 1);
        var followUpQuestion = test.Questions.Single(q => q.Code == "BRANCH2-FOLLOWUP");

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Nazarova Sevinch Aliyevna", new DateOnly(2010, 4, 4));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/BRANCH2/start", UriKind.Relative), content: null);

        var request = new { answers = new[] { new { questionId = followUpQuestion.Id, text = "hali ko'rinmasligi kerak", durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/BRANCH2/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("QUESTION_NOT_VISIBLE");
    }
}

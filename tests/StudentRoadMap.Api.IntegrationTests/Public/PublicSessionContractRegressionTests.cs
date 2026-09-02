using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.Public.StartTest;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// QA ko'rigidan keyin qo'shilgan shartnoma regressiya testlari (P12).
///
/// Alohida `IClassFixture` — `PublicStartSession` rate limiter kvotasi (10/soat/IP) boshqa test
/// klasslari bilan bo'linmasligi uchun.
/// </summary>
public sealed class PublicSessionContractRegressionTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSessionContractRegressionTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> StartSessionAsync(
        HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    /// <summary>
    /// `GET /sessions/me` javobida test NOMI va taxminiy vaqti bo'lishi shart. Ularsiz frontend
    /// nomni landing javobidan `localStorage`ga saqlab qo'yishga majbur bo'ladi va o'quvchi
    /// to'g'ridan-to'g'ri test havolasiga kirsa nom yo'qoladi.
    /// </summary>
    [Fact]
    public async Task GetSessionState_TestBloklarida_NomVaTaxminiyVaqtQaytadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("cr1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-cr1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "CR1", 1, questionCount: 4, pageSize: 2);

        var client = _factory.CreateClient();
        var token = await StartSessionAsync(client, school, accessToken, "Nomli Test Oquvchi", new DateOnly(2010, 5, 5));
        client.DefaultRequestHeaders.Add("X-Session-Token", token);

        var state = await client.GetFromJsonAsync<GetSessionStateResult>("/api/public/sessions/me", TestJson.Options);

        state!.Tests.Should().NotBeEmpty();
        var block = state.Tests[0];
        block.Name.Should().NotBeNullOrWhiteSpace();
        block.Name.Should().NotBe(block.Code, "nom kod bilan bir xil bo'lmasligi kerak — u haqiqiy NameUz dan keladi");
        block.EstimatedMinutes.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// `POST /tests/{code}/start` idempotent: ikkinchi chaqiruv xato bermaydi VA savol tartibini
    /// qayta aralashtirmaydi. Aks holda o'quvchining savollari test o'rtasida joyini o'zgartiradi.
    /// </summary>
    [Fact]
    public async Task StartTest_IkkiMartaChaqirilsa_SavolTartibiOzgarmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("cr2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-cr2", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(
            db, now, "CR2", 1, questionCount: 8, pageSize: 8, shuffleQuestions: true);

        var client = _factory.CreateClient();
        var token = await StartSessionAsync(client, school, accessToken, "Idempotent Start Oquvchi", new DateOnly(2010, 6, 6));
        client.DefaultRequestHeaders.Add("X-Session-Token", token);

        var first = await client.PostAsync("/api/public/sessions/tests/CR2/start", null);
        first.EnsureSuccessStatusCode();
        var firstQuestions = await client.GetFromJsonAsync<GetTestQuestionsResult>(
            "/api/public/sessions/tests/CR2/questions?page=1", TestJson.Options);

        // Ikkinchi `start` — xato bermasligi va tartibni buzmasligi shart.
        var second = await client.PostAsync("/api/public/sessions/tests/CR2/start", null);
        second.EnsureSuccessStatusCode();
        var secondBody = await second.Content.ReadFromJsonAsync<StartTestResult>(TestJson.Options);
        secondBody!.TestCode.Should().Be("CR2");

        var secondQuestions = await client.GetFromJsonAsync<GetTestQuestionsResult>(
            "/api/public/sessions/tests/CR2/questions?page=1", TestJson.Options);

        secondQuestions!.Questions.Select(q => q.Id)
            .Should().Equal(firstQuestions!.Questions.Select(q => q.Id));
    }
}

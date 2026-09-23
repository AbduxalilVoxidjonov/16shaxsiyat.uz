using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST /api/public/sessions` IP bo'yicha soatiga 10 ta chegara (`docs/07` 4-bo'lim jadvali).
/// **Alohida test klassi** — `PublicApiTestFactory` klass darajasida bitta marta yaratiladi
/// (`IClassFixture`), rate limiter holati esa butun host umri davomida saqlanadi; agar bu test
/// boshqa testlar bilan bitta klassda bo'lsa, ularning `POST /sessions` chaqiruvlari shu
/// kvotani "yeb qo'yib", testni beqaror qilardi — shu sabab bu yakka o'zi alohida klassda.
/// </summary>
public sealed class PublicSessionRateLimitTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSessionRateLimitTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_11InchiSorov_429Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("ratelimit1");
        // Kunlik limit rate-limiterdan oldin ishga tushmasligi uchun baland qo'yiladi.
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-ratelimit1", token, dailyRegistrationLimit: 1000);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "RL1", 1, questionCount: 1);

        using var client = _factory.CreateClient();

        StartSessionCommand Command(int i) => new(
            school.Slug.Value, token, null, $"Foydalanuvchi RateLimit Familiyasi{i}",
            new DateOnly(2010, 1, 1), Gender.Male, 9, "A", "+998901234567", "+998909998877", null, true, "uz");

        var statuses = new List<HttpStatusCode>();
        for (var i = 1; i <= 11; i++)
        {
            var response = await client.PostAsJsonAsync("/api/public/sessions", Command(i), TestJson.Options);
            statuses.Add(response.StatusCode);
        }

        statuses.Take(10).Should().NotContain(HttpStatusCode.TooManyRequests, "birinchi 10 ta so'rov limit ichida bo'lishi kerak");
        statuses[10].Should().Be(HttpStatusCode.TooManyRequests, "11-so'rov soatlik IP limitidan (10) oshadi");
    }
}

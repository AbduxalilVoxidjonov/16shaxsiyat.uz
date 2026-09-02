using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// `POST /api/auth/login` IP bo'yicha 10/5 daqiqa (`RateLimitSetup.AdminLogin`,
/// `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 9-band). **Alohida test klassi** — o'z `IClassFixture`
/// nusxasi (rate limiter holati host umri davomida saqlanadi), aks holda boshqa Auth testlari
/// bilan bitta klassda bo'lsa ularning so'rovlari shu kvotani "yeb qo'yardi"
/// (`PublicSessionRateLimitTests`dagi bilan bir xil naqsh/sabab).
/// </summary>
public sealed class AuthLoginRateLimitTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthLoginRateLimitTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_OnBirinchiSorov_429Qaytaradi()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "rl-nonexistent", password = "IstalganParol1!" },
                TestJson.Options);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var problem = await lastResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("RATE_LIMITED");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST /api/public/schools/resolve-code` IP bo'yicha 10/5 daqiqa
/// (`RateLimitSetup.PublicResolveSchoolCode`, `AdminLogin` bilan bir xil qattiqlik).
/// **Alohida test klassi** — o'z `IClassFixture` nusxasi (`AuthLoginRateLimitTests` bilan bir
/// xil sabab: limiter holati host umri davomida saqlanadi).
/// </summary>
public sealed class PublicResolveSchoolCodeRateLimitTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicResolveSchoolCodeRateLimitTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResolveCode_OnBirinchiSorov_429Qaytaradi()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.PostAsJsonAsync(
                "/api/public/schools/resolve-code",
                new { code = "YUQQ2345" },
                TestJson.Options);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.RetryAfter.Should().NotBeNull("mijoz qancha kutishini bilishi kerak");
        var problem = await lastResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("RATE_LIMITED");
    }
}

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StudentRoadMap.Api.IntegrationTests;

/// <summary>
/// `/health` va `/health/ready` endpointlarini WebApplicationFactory orqali tekshiradi —
/// P01 skeletining ishga tushishini tasdiqlovchi smoke test.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task Health_endpoint_200_qaytaradi(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StudentRoadMap.Api.IntegrationTests;

/// <summary>
/// `/health` va `/health/ready` endpointlarini WebApplicationFactory orqali tekshiradi.
///
/// P03 dan boshlab `/health/ready` DB ulanishini tekshiradi (`AddDbContextCheck`) — bu muhitda
/// (CI/lokal, jonli PostgreSQL'siz) ulanish muvaffaqiyatsiz bo'lishi kutiladi, shu sabab
/// `503` qaytarishi tasdiqlanadi (`prompts/03` DoD: "`/health/ready` DB o'chirilganda 503
/// qaytaradi"). `/health` esa DB'siz — har doim `200` (jonlik, `docs/06` bo'yicha).
/// Ulanish satri sinov host konfiguratsiyasida (tez muvaffaqiyatsizlik uchun qisqa timeout
/// bilan) beriladi — `appsettings.json`/kodda emas (`CLAUDE.md` 4-qoida).
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<HealthEndpointTests.TestFactory>
{
    private readonly TestFactory _factory;

    public HealthEndpointTests(TestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_har_doim_200_qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_db_ulanmasa_503_qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>Sinov uchun (muhitda jonli DB yo'q) qisqa timeoutli, mavjud bo'lmagan DB'ga ulanish satri.</summary>
    public sealed class TestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=127.0.0.1;Port=5432;Database=studentroadmap;Username=srm;Password=test;Timeout=1;Command Timeout=1",
                    // `Program.cs` `IJwtTokenService`ni START-UPda MAJBURIY resolve qiladi
                    // (fail-fast, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band) — bu factory
                    // `PublicApiTestFactory`dan meros olmagani uchun qiymat shu yerda alohida beriladi.
                    ["Jwt:Key"] = "integration-test-jwt-signing-key-at-least-32-bytes-long-0000",
                });
            });
        }
    }
}

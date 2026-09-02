using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// TOTP asosiy kod bilan login — to'g'ri kod, noto'g'ri kod va qayta ishlatishga qarshi himoya
/// (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 5-band). Alohida klass — `AuthTotpEnableEndpointTests`
/// izohiga qarang (login rate-limit kvotasi).
/// </summary>
public sealed class AuthTotpCodeEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthTotpCodeEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ToGriTotpKodBilan_200Qaytaradi_VaKodniQaytaIshlatibBolmaydi()
    {
        const string username = "totp-valid-code";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        var enableResult = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var now = DateTimeOffset.UtcNow;
        var code = TotpTestHelper.ComputeCode(enableResult.Secret, now);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword, totpCode = code },
            TestJson.Options);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Xuddi shu kod bilan darhol qayta urinish — qayta ishlatishga qarshi himoya rad etishi kerak.
        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword, totpCode = code },
            TestJson.Options);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NotogriTotpKodBilan_401Qaytaradi()
    {
        const string username = "totp-wrong-code";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword, totpCode = "000000" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

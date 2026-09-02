using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// TOTP zaxira kodlari va `POST /api/auth/totp/disable` — `docs/08` 2-bo'lim. Alohida klass —
/// `AuthTotpEnableEndpointTests` izohiga qarang (login rate-limit kvotasi).
/// </summary>
public sealed class AuthTotpBackupAndDisableEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthTotpBackupAndDisableEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ZaxiraKodBilan_200Qaytaradi_VaKodBirMartaIshlatiladi()
    {
        const string username = "totp-backup-code";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        var enableResult = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);
        var backupCode = enableResult.BackupCodes[0];

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword, totpCode = backupCode },
            TestJson.Options);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword, totpCode = backupCode },
            TestJson.Options);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "zaxira kod bir martalik — ikkinchi marta ishlamasligi kerak");
    }

    [Fact]
    public async Task DisableTotp_ToGriParolBilan_TotpniOchiradi()
    {
        const string username = "totp-disable";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var disableResponse = await client.PostAsJsonAsync(
            "/api/auth/totp/disable",
            new { currentPassword = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.DefaultRequestHeaders.Authorization = null;

        // Endi TOTP kodisiz oddiy login ishlashi kerak.
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DisableTotp_NotogriParolBilan_401Qaytaradi()
    {
        const string username = "totp-disable-wrong";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/api/auth/totp/disable",
            new { currentPassword = "NotogriParol1!" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

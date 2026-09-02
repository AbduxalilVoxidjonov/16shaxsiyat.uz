using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// TOTP yoqish va yoqilgandan keyin kod talab qilinishi (`docs/08` 2-bo'lim).
/// **Alohida klass** — login rate-limit kvotasini (`AdminLogin`, 10/5 daq) boshqa TOTP
/// testlari bilan bo'lishmasin deb (bitta klassda ko'p login chaqiruvi kvotani tugatgan
/// edi — shu sabab TOTP testlari bir necha kichik klassga bo'lingan).
/// </summary>
public sealed class AuthTotpEnableEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthTotpEnableEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnableTotp_SakkizTaZaxiraKodQaytaradi()
    {
        const string username = "totp-enable";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        var enableResult = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        enableResult.Secret.Should().NotBeNullOrWhiteSpace();
        enableResult.OtpauthUri.Should().StartWith("otpauth://totp/");
        enableResult.BackupCodes.Should().HaveCount(8);
        enableResult.BackupCodes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Login_TotpYoqilganHisobKodsiz_401TotpRequiredQaytaradi()
    {
        const string username = "totp-required";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_REQUIRED");
    }
}

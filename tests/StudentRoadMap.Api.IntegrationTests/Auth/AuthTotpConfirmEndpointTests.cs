using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// TOTP o'rnatishning 2-bosqichi (`POST /api/auth/totp/confirm`) — 2FA faqat shu yerda
/// yoqiladi. Alohida klass — `AuthTotpEnableEndpointTests` izohiga qarang (login rate-limit).
/// </summary>
public sealed class AuthTotpConfirmEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthTotpConfirmEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfirmTotp_ToGriKodBilan_TotpniYoqadi_VaSakkizTaZaxiraKodQaytaradi()
    {
        const string username = "totp-confirm-ok";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        var (_, confirm) = await TotpTestSupport.EnrollTotpAsync(client, login.AccessToken);

        confirm.BackupCodes.Should().HaveCount(8);
        confirm.BackupCodes.Should().OnlyHaveUniqueItems();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_REQUIRED");
    }

    [Fact]
    public async Task ConfirmTotp_NotogriKodBilan_400TotpCodeInvalidQaytaradi_VaTotpYoqilmaydi()
    {
        const string username = "totp-confirm-wrong";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var response = await TotpTestSupport.ConfirmTotpRawAsync(client, login.AccessToken, "000000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_CODE_INVALID");

        // 2FA yoqilmagan — kodsiz login hamon ishlaydi.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmTotp_EnableChaqirilmasdan_409EnrollmentNotStartedQaytaradi()
    {
        const string username = "totp-confirm-nostart";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        var response = await TotpTestSupport.ConfirmTotpRawAsync(client, login.AccessToken, "123456");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_ENROLLMENT_NOT_STARTED");
    }

    [Fact]
    public async Task ConfirmTotp_MuddatiOtganOrnatish_409EnrollmentExpiredQaytaradi()
    {
        const string username = "totp-confirm-expired";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        var enable = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        await TotpTestSupport.ExpirePendingEnrollmentAsync(_factory, username);

        var code = TotpTestHelper.ComputeCode(enable.Secret, DateTimeOffset.UtcNow);
        var response = await TotpTestSupport.ConfirmTotpRawAsync(client, login.AccessToken, code);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_ENROLLMENT_EXPIRED");
    }

    [Fact]
    public async Task ConfirmTotp_OltiXonaliBolmaganKod_400ValidationErrorQaytaradi()
    {
        const string username = "totp-confirm-format";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var response = await TotpTestSupport.ConfirmTotpRawAsync(client, login.AccessToken, "12ab");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ConfirmTotp_TotpAllaqachonYoqilgan_409Qaytaradi()
    {
        const string username = "totp-confirm-already";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);
        var (enable, _) = await TotpTestSupport.EnrollTotpAsync(client, login.AccessToken);

        var response = await TotpTestSupport.ConfirmTotpRawAsync(
            client,
            login.AccessToken,
            TotpTestSupport.NextLoginCode(enable.Secret));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TOTP_ALREADY_ENABLED");
    }
}

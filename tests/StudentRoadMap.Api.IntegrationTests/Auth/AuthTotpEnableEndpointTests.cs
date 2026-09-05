using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// TOTP o'rnatishning 1-bosqichi (`POST /api/auth/totp/enable`, `docs/08` 2-bo'lim).
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
    public async Task EnableTotp_QrKodVaSirQaytaradi()
    {
        const string username = "totp-enable";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        var enableResult = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        enableResult.Secret.Should().NotBeNullOrWhiteSpace();
        enableResult.OtpauthUri.Should().StartWith("otpauth://totp/");
        enableResult.OtpauthUri.Should().Contain(enableResult.Secret);
        enableResult.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Maktab QR bilan bir xil format — xom base64 PNG (`data:` prefiksisiz).
        enableResult.QrCodePngBase64.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(enableResult.QrCodePngBase64);
        bytes.Should().StartWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "PNG imzosi bo'lishi kerak");
    }

    /// <summary>
    /// Asosiy regressiya himoyasi: `enable` 2FA'ni YOQMAYDI. Ilgari yoqar edi va o'rnatish
    /// muvaffaqiyatsiz bo'lsa hisob keyingi kirishda butunlay bloklanardi.
    /// </summary>
    [Fact]
    public async Task EnableTotp_TotpniYoqmaydi_LoginKodsizIshlaydi()
    {
        const string username = "totp-enable-pending";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "tasdiqlanmaguncha 2FA yoqilmaydi");
    }

    [Fact]
    public async Task EnableTotp_QaytaChaqirilganda_YangiSirBeradi()
    {
        const string username = "totp-enable-twice";
        await TotpTestSupport.SeedAdminAsync(_factory, username);
        using var client = _factory.CreateClient();
        var login = await TotpTestSupport.LoginWithoutTotpAsync(client, username);

        var first = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);
        var second = await TotpTestSupport.EnableTotpAsync(client, login.AccessToken);

        second.Secret.Should().NotBe(first.Secret);

        // Eski (almashtirilgan) sir bilan tasdiqlab bo'lmaydi.
        var staleCode = TotpTestHelper.ComputeCode(first.Secret, DateTimeOffset.UtcNow);
        var response = await TotpTestSupport.ConfirmTotpRawAsync(client, login.AccessToken, staleCode);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

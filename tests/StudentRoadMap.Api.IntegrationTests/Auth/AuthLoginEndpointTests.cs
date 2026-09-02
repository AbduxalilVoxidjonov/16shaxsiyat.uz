using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// `POST /api/auth/login` — `docs/07` 2-bo'lim, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 1-band
/// (timing/xabar oshkor qilmaslik). Login rate-limit siyosati (`AdminLogin`) bu klassdagi har
/// bir testda kamida bitta so'rov sarflaydi — 10/5 daqiqa limitidan pastda qoladi, shu sabab
/// bitta klassda birga (alohida `AuthLoginRateLimitTests` esa kvotani ATAYLAB tugatadi).
/// </summary>
public sealed class AuthLoginEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthLoginEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAdminAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username, password);
    }

    [Fact]
    public async Task Login_ToGriMalumot_200VaAccessTokenQaytaradi()
    {
        await SeedAdminAsync("login-ok", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "login-ok", password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().BeGreaterThan(0);
        body.User.Username.Should().Be("login-ok");
        body.User.Role.Should().Be("SuperAdmin");

        // Refresh token javob TANASIDA hech qachon bo'lmasligi kerak (`docs/08` 2-bo'lim).
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("refreshToken", "refresh token faqat httpOnly cookie orqali yuboriladi");
    }

    [Fact]
    public async Task Login_ToGriMalumot_RefreshTokenHttpOnlySecureStrictCookieDaQaytadi()
    {
        await SeedAdminAsync("login-cookie", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "login-cookie", password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var refreshCookie = cookies!.Should().ContainSingle(c => c.StartsWith("srm_refresh_token=", StringComparison.Ordinal)).Subject;

        refreshCookie.Should().ContainEquivalentOf("HttpOnly", "cookie HttpOnly bayrog'i bilan kelishi shart");
        refreshCookie.Should().ContainEquivalentOf("Secure");
        refreshCookie.Should().ContainEquivalentOf("SameSite=Strict");
        refreshCookie.Should().ContainEquivalentOf("path=/api/auth", "cookie faqat /api/auth ostiga cheklangan");
    }

    [Fact]
    public async Task Login_NotogriParol_GenerikXatoQaytaradi()
    {
        await SeedAdminAsync("login-wrongpwd", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "login-wrongpwd", password = "BoshqaParol1!" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("UNAUTHORIZED");
        problem.GetProperty("title").GetString().Should().Be("Login yoki parol noto'g'ri.");
    }

    [Fact]
    public async Task Login_MavjudBolmaganFoydalanuvchi_NotogriParolBilanBirXilJavobQaytaradi()
    {
        await SeedAdminAsync("login-exists-only", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();

        // Bir xil klient/host — ikkalasi ham bir xil status + bir xil `code`/`title` olishi kerak
        // (MAXSUS DIQQAT 1-band: qaysi maydon noto'g'riligi oshkor bo'lmasin).
        var unknownUserResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "login-mavjud-emas", password = "IstalganParol1!" },
            TestJson.Options);
        var wrongPasswordResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "login-exists-only", password = "IstalganParol1!" },
            TestJson.Options);

        unknownUserResponse.StatusCode.Should().Be(wrongPasswordResponse.StatusCode);

        var unknownProblem = await unknownUserResponse.Content.ReadFromJsonAsync<JsonElement>();
        var wrongPasswordProblem = await wrongPasswordResponse.Content.ReadFromJsonAsync<JsonElement>();
        unknownProblem.GetProperty("code").GetString().Should().Be(wrongPasswordProblem.GetProperty("code").GetString());
        unknownProblem.GetProperty("title").GetString().Should().Be(wrongPasswordProblem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_BosQiymatlar_400ValidationErrorQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "", password = "" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }
}

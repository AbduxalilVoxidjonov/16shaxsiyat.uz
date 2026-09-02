using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// `GET /api/auth/me` va `POST /api/auth/change-password` — `[Authorize(Policy = "SuperAdmin")]`
/// himoyasi (token bilan 200, tokensiz 401) va parol o'zgarganda BARCHA refresh tokenlar bekor
/// qilinishi (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 7-band).
/// </summary>
public sealed class AuthMeAndChangePasswordEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthMeAndChangePasswordEndpointTests(PublicApiTestFactory factory)
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

    private static async Task<(LoginResult Body, string Cookie)> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password }, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options);
        var cookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_refresh_token");
        return (body!, cookie!);
    }

    [Fact]
    public async Task Me_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ToGriAccessTokenBilan_200VaFoydalanuvchiMalumotiniQaytaradi()
    {
        await SeedAdminAsync("me-ok", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();
        var (login, _) = await LoginAsync(client, "me-ok", AdminTestDataFactory.DefaultPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDto>(TestJson.Options);
        body!.Username.Should().Be("me-ok");
    }

    [Fact]
    public async Task Me_YaroqsizTokenBilan_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ToGriJoriyParol_TokenlarniBekorQiladi()
    {
        await SeedAdminAsync("changepwd-ok", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();
        var (login, cookie) = await LoginAsync(client, "changepwd-ok", AdminTestDataFactory.DefaultPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = AdminTestDataFactory.DefaultPassword, newPassword = "YangiParol1Kuchli!" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Eski refresh token endi ishlamasligi kerak.
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", cookie } } };
        var refreshAfterChange = await client.SendAsync(refreshRequest);
        refreshAfterChange.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Yangi parol bilan kirish ishlashi kerak.
        client.DefaultRequestHeaders.Authorization = null;
        var newLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "changepwd-ok", password = "YangiParol1Kuchli!" },
            TestJson.Options);
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_NotogriJoriyParol_401Qaytaradi()
    {
        await SeedAdminAsync("changepwd-wrong", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();
        var (login, _) = await LoginAsync(client, "changepwd-wrong", AdminTestDataFactory.DefaultPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = "NotogriJoriyParol1!", newPassword = "YangiParol1Kuchli!" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_QisqaYangiParol_400ValidationErrorQaytaradi()
    {
        await SeedAdminAsync("changepwd-weak", AdminTestDataFactory.DefaultPassword);
        using var client = _factory.CreateClient();
        var (login, _) = await LoginAsync(client, "changepwd-weak", AdminTestDataFactory.DefaultPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = AdminTestDataFactory.DefaultPassword, newPassword = "short" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

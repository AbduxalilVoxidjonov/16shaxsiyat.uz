using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Identity.Refresh;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// `POST /api/auth/refresh` — rotatsiya va qayta ishlatishni aniqlash (`docs/08` 2-bo'lim,
/// `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 3-band). Bu — eng muhim xavfsizlik testi:
/// o'g'irlangan (eski, allaqachon rotatsiyalangan) refresh token bilan urinish foydalanuvchining
/// BARCHA tokenlarini bekor qilishi va `Security.RefreshReuse` audit yozuvi qoldirishi kerak.
/// </summary>
public sealed class AuthRefreshEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthRefreshEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(string Username, Guid AdminUserId)> SeedAdminAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        return (username, admin.Id);
    }

    private static async Task<(LoginResult Body, string CookieValue)> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options);
        var cookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_refresh_token");
        cookie.Should().NotBeNull();

        return (body!, cookie!);
    }

    private static HttpRequestMessage RefreshRequest(string cookieValue) =>
        new(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { { "Cookie", cookieValue } },
        };

    [Fact]
    public async Task Refresh_ToGriCookieBilan_YangiAccessTokenVaRotatsiyaQilinganCookieQaytaradi()
    {
        await SeedAdminAsync("refresh-ok");
        using var client = _factory.CreateClient();
        var (loginBody, cookie) = await LoginAsync(client, "refresh-ok");

        var response = await client.SendAsync(RefreshRequest(cookie));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RefreshResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.AccessToken.Should().NotBe(loginBody.AccessToken);

        var newCookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_refresh_token");
        newCookie.Should().NotBeNull();
        newCookie.Should().NotBe(cookie, "rotatsiya — har chaqiruvda yangi refresh token beriladi");
    }

    [Fact]
    public async Task Refresh_EskiBekorQilinganTokenBilan_BarchaTokenlarniBekorQiladiVaAuditYozadi()
    {
        var (username, adminUserId) = await SeedAdminAsync("refresh-reuse");
        using var client = _factory.CreateClient();
        var (_, firstCookie) = await LoginAsync(client, username);

        // Rotatsiya — `firstCookie` endi BEKOR QILINGAN, `secondCookie` FAOL.
        var rotateResponse = await client.SendAsync(RefreshRequest(firstCookie));
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondCookie = AdminTestDataFactory.ExtractCookieValue(rotateResponse, "srm_refresh_token");

        // Hujumchi eski (bekor qilingan) tokenni qayta ishlatishga urinadi.
        var reuseResponse = await client.SendAsync(RefreshRequest(firstCookie));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Haqiqiy foydalanuvchining YANGI (rotatsiyadan keyingi) tokeni ham endi ishlamasligi
        // kerak — qayta ishlatish aniqlanganda BARCHA tokenlar bekor qilinadi.
        var secondAttempt = await client.SendAsync(RefreshRequest(secondCookie!));
        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var allTokensRevoked = await db.RefreshTokens
            .Where(t => t.AdminUserId == adminUserId)
            .AllAsync(t => t.RevokedAt != null);
        allTokensRevoked.Should().BeTrue("qayta ishlatish aniqlanganda foydalanuvchining BARCHA tokenlari bekor qilinishi shart");

        var auditExists = await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.SecurityRefreshReuse && a.AdminUserId == adminUserId);
        auditExists.Should().BeTrue("`Security.RefreshReuse` audit yozuvi qoldirilishi shart");
    }

    [Fact]
    public async Task Refresh_CookieSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/auth/refresh", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RefreshTokenniBekorQiladiVaCookieniTozalaydi()
    {
        await SeedAdminAsync("logout-ok");
        using var client = _factory.CreateClient();
        var (_, cookie) = await LoginAsync(client, "logout-ok");

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Headers = { { "Cookie", cookie } },
        };
        var logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCookie = AdminTestDataFactory.ExtractCookieValue(logoutResponse, "srm_refresh_token");
        deletedCookie.Should().NotBeNull();

        // Bekor qilingan token bilan refresh endi ishlamasligi kerak.
        var refreshAfterLogout = await client.SendAsync(RefreshRequest(cookie));
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

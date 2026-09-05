using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Application.PublicUsers.Refresh;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// `POST /api/auth/telegram/refresh` va `/logout` — superadmin oqimidagi
/// (`AuthRefreshEndpointTests`) bilan AYNAN bir xil xavfsizlik xatti-harakati:
/// rotatsiya + qayta ishlatishni aniqlash (`docs/08` 2-bo'lim).
/// </summary>
public sealed class PublicRefreshEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private readonly TelegramApiTestFactory _factory;

    public PublicRefreshEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private static HttpRequestMessage RefreshRequest(string cookieValue) =>
        new(HttpMethod.Post, "/api/auth/telegram/refresh")
        {
            Headers = { { "Cookie", cookieValue } },
        };

    [Fact]
    public async Task Refresh_ToGriCookieBilan_YangiTokenVaRotatsiyaQilinganCookieQaytaradi()
    {
        using var client = _factory.CreateClient();
        var (accessToken, cookie, _) = await PublicUserTestDataFactory.LoginAsync(client, 710100001);

        var response = await client.SendAsync(RefreshRequest(cookie));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PublicRefreshResult>(TestJson.Options);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.AccessToken.Should().NotBe(accessToken);

        var newCookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_public_refresh_token");
        newCookie.Should().NotBeNull();
        newCookie.Should().NotBe(cookie, "rotatsiya — har chaqiruvda yangi refresh token beriladi");
    }

    [Fact]
    public async Task Refresh_EskiBekorQilinganTokenBilan_BarchaTokenlarniBekorQiladiVaAuditYozadi()
    {
        using var client = _factory.CreateClient();
        var (_, firstCookie, user) = await PublicUserTestDataFactory.LoginAsync(client, 710100002);

        var rotateResponse = await client.SendAsync(RefreshRequest(firstCookie));
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondCookie = AdminTestDataFactory.ExtractCookieValue(rotateResponse, "srm_public_refresh_token");

        // Hujumchi eski (bekor qilingan) tokenni qayta ishlatishga urinadi.
        var reuse = await client.SendAsync(RefreshRequest(firstCookie));
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Haqiqiy foydalanuvchining YANGI tokeni ham endi ishlamasligi kerak.
        var afterReuse = await client.SendAsync(RefreshRequest(secondCookie!));
        afterReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var allRevoked = await db.PublicRefreshTokens
            .Where(t => t.PublicUserId == user.Id)
            .AllAsync(t => t.RevokedAt != null);
        allRevoked.Should().BeTrue("qayta ishlatish aniqlanganda BARCHA tokenlar bekor qilinadi");

        var auditExists = await db.AuditLogs.AnyAsync(a =>
            a.Action == PublicAuditActions.RefreshReuse && a.EntityId == user.Id);
        auditExists.Should().BeTrue("`PublicSecurity.RefreshReuse` audit yozuvi qoldirilishi shart");
    }

    [Fact]
    public async Task Refresh_CookieSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/auth/telegram/refresh", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_SuperadminCookieNomiBilanKelgan_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        var (_, cookie, _) = await PublicUserTestDataFactory.LoginAsync(client, 710100003);

        // Ommaviy token superadmin cookie NOMI ostida yuborilsa ham o'qilmaydi — ikki oqim
        // cookie nomi bilan ham ajratilgan (`PublicRefreshTokenCookie` izohi).
        var renamed = cookie.Replace("srm_public_refresh_token=", "srm_refresh_token=", StringComparison.Ordinal);

        var response = await client.SendAsync(RefreshRequest(renamed));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RefreshTokenniBekorQiladiVaCookieniTozalaydi()
    {
        using var client = _factory.CreateClient();
        var (_, cookie, _) = await PublicUserTestDataFactory.LoginAsync(client, 710100004);

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/telegram/logout")
        {
            Headers = { { "Cookie", cookie } },
        };
        var logoutResponse = await client.SendAsync(logout);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AdminTestDataFactory.ExtractCookieValue(logoutResponse, "srm_public_refresh_token").Should().NotBeNull();

        var afterLogout = await client.SendAsync(RefreshRequest(cookie));
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_CookieSiz_204Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/auth/telegram/logout", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

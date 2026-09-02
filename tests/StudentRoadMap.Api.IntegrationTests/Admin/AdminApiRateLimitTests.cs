using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `docs/07-api-shartnoma.md` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`,
/// himoyalangan `SchoolsController`/`StudentsController`/`AuthController` endpointlariga qo'llanadi
/// (koordinator qarori, 2026-09-02). **Alohida test klassi** — `PublicSessionRateLimitTests`dagi
/// bilan bir xil sabab: rate limiter holati butun host umri davomida saqlanadi, boshqa
/// testlarning `GET /api/admin/schools` chaqiruvlari shu kvotani "yeb qo'ymasligi" uchun.
/// </summary>
public sealed class AdminApiRateLimitTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminApiRateLimitTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSchoolsList_301InchiSorov_429Qaytaradi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, "admin-ratelimit-user");
        }

        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin-ratelimit-user", password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var statuses = new List<HttpStatusCode>();
        for (var i = 1; i <= 301; i++)
        {
            var response = await client.GetAsync(new Uri("/api/admin/schools?pageSize=1", UriKind.Relative));
            statuses.Add(response.StatusCode);
        }

        statuses.Take(300).Should().NotContain(HttpStatusCode.TooManyRequests, "birinchi 300 ta so'rov limit ichida bo'lishi kerak");
        statuses[300].Should().Be(HttpStatusCode.TooManyRequests, "301-so'rov daqiqalik IP limitidan (300) oshadi");
    }
}

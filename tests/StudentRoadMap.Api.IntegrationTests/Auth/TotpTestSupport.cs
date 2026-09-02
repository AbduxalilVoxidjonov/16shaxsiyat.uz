using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.EnableTotp;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>TOTP testlari uchun umumiy yordamchilar (seed/login/enable) — takrorlanishni kamaytiradi.</summary>
internal static class TotpTestSupport
{
    public static async Task SeedAdminAsync(PublicApiTestFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
    }

    public static async Task<LoginResult> LoginWithoutTotpAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;
    }

    public static async Task<EnableTotpResult> EnableTotpAsync(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.PostAsync(new Uri("/api/auth/totp/enable", UriKind.Relative), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<EnableTotpResult>(TestJson.Options))!;
        client.DefaultRequestHeaders.Authorization = null;

        return body;
    }
}

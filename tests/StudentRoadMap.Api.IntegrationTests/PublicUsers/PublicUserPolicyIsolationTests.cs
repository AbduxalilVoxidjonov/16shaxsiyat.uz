using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// **Ikki auditoriyaning ajratilishi** — P47ning eng muhim xavfsizlik talabi:
/// ommaviy token superadmin endpointlariga, superadmin tokeni esa ommaviy kabinetga
/// KIRA OLMASLIGI kerak. Himoya ikki qatlamli: alohida `aud`
/// (`Jwt:Audience` / `Jwt:PublicAudience` — imzo validatsiyasi darajasida) va alohida rol
/// (`SuperAdmin` / `PublicUser`).
/// </summary>
public sealed class PublicUserPolicyIsolationTests : IClassFixture<TelegramApiTestFactory>
{
    private readonly TelegramApiTestFactory _factory;

    public PublicUserPolicyIsolationTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> AdminAccessTokenAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!.AccessToken;
    }

    [Fact]
    public async Task OmmaviyToken_SuperadminEndpointiga_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        var (publicAccessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, 750100001);
        client.UseBearer(publicAccessToken);

        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "ommaviy tokenning `aud`i superadmin sxemasida validatsiyadan o'tmaydi — rolga umuman yetib bormaydi");
    }

    [Fact]
    public async Task OmmaviyToken_AdminMaktablarRoyxatiga_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        var (publicAccessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, 750100002);
        client.UseBearer(publicAccessToken);

        var response = await client.GetAsync(new Uri("/api/admin/schools", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuperadminToken_OmmaviyKabinetga_401Qaytaradi()
    {
        var adminAccessToken = await AdminAccessTokenAsync("izolyatsiya-admin");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var meResponse = await client.GetAsync(new Uri("/api/me", UriKind.Relative));
        var assessmentsResponse = await client.GetAsync(new Uri("/api/me/assessments", UriKind.Relative));

        meResponse.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "superadmin tokenining `aud`i ommaviy sxemada validatsiyadan o'tmaydi");
        assessmentsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuperadminToken_OzEndpointidaIshlaydi_XattiHarakatOzgarmadi()
    {
        var adminAccessToken = await AdminAccessTokenAsync("izolyatsiya-admin-ok");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "superadmin oqimi P47da o'zgarmasligi SHART");
    }

    /// <summary>
    /// STRUKTURA darajasidagi qulf (`PublicRouteIdorGuardTests` naqshi): `/api/me/*` ning
    /// HAR BIR marshruti `PublicUserPolicy` talab qilishi kerak — kelajakda kimdir
    /// autentifikatsiyasiz endpoint qo'shsa, xatti-harakat testi yozilmagan bo'lsa ham
    /// shu yerda darhol qizil bo'ladi.
    /// </summary>
    [Fact]
    public async Task ApiMeMarshrutlari_HammasiPublicUserPolicyTalabQiladi()
    {
        await Task.CompletedTask;
        _ = _factory.Server;

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/me", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        endpoints.Should().NotBeEmpty("kabinet endpointlari topilishi kerak — aks holda test hech nimani tekshirmaydi");

        foreach (var endpoint in endpoints)
        {
            var authorize = endpoint.Metadata.GetMetadata<IAuthorizeData>();

            authorize.Should().NotBeNull("`{0}` autentifikatsiyasiz qolmasligi kerak", endpoint.RoutePattern.RawText);
            authorize!.Policy.Should().Be(
                JwtAuthenticationSetup.PublicUserPolicy,
                "`{0}` faqat ommaviy foydalanuvchi siyosatini qabul qilishi kerak",
                endpoint.RoutePattern.RawText);
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `SchoolsController.List` — `docs/07` 3.1-bo'lim, `prompts/14`. Alohida `IClassFixture`
/// (rate limiter kvotasi boshqa admin test klasslaridan aralashmasligi uchun — `AdminLogin`
/// siyosati IP bo'yicha 10/5 daqiqa, `CLAUDE.md`/vazifa ko'rsatmasi: "bu tuzoqqa uch marta
/// tushilgan").
/// </summary>
public sealed class AdminSchoolsListEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolsListEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    [Fact]
    public async Task List_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/schools", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_SahifalashVaFiltr_ToGriPagedResultQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        for (var i = 1; i <= 3; i++)
        {
            await TestDataFactory.CreateSchoolAsync(db, now, $"list-toshkent-{i}", TestDataFactory.NewAccessToken($"list-t-{i}"));
        }

        var samarqand = await TestDataFactory.CreateSchoolAsync(
            db, now, "list-samarqand-1", TestDataFactory.NewAccessToken("list-s-1"), region: "Samarqand");

        using var client = await AuthenticatedClientAsync("schools-list-admin");

        var page1 = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?page=1&pageSize=2&sort=name", TestJson.Options);
        page1.Should().NotBeNull();
        page1!.PageSize.Should().Be(2);
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(4);
        page1.HasNext.Should().BeTrue();

        var byRegion = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?region=Samarqand", TestJson.Options);
        byRegion!.Items.Should().ContainSingle(s => s.Id == samarqand.Id);

        var bySearch = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?search=samarqand-1", TestJson.Options);
        bySearch!.Items.Should().Contain(s => s.Id == samarqand.Id);
    }

    [Fact]
    public async Task List_PageSize100danKatta_JimginaYuzgaTushiriladi()
    {
        using var client = await AuthenticatedClientAsync("schools-pagesize-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?pageSize=500", TestJson.Options);

        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetById_MavjudEmas_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("schools-getbyid-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/schools/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

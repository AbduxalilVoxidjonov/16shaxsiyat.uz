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
/// `SchoolsController.List` — `sort=createdAt` (DB-darajasidagi `ORDER BY`, `DateTimeOffset`
/// ustun). Avval SQLite'da `Skip` qilingan edi (`DateTimeOffset` `ORDER BY` tarjima
/// qilinmasligi sababli) — `AppDbContext.ApplySqliteDateTimeOffsetConversion` bilan yopildi
/// (`prompts/15` 2-bosqich, 2026-09-02), endi to'liq ishlaydi.
/// </summary>
public sealed class AdminSchoolsSortEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolsSortEndpointTests(PublicApiTestFactory factory)
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
    public async Task List_SortCreatedAt_DBDarajasidaTogriTartiblaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var first = await TestDataFactory.CreateSchoolAsync(db, now.AddMinutes(-10), "schools-sort-created-1", TestDataFactory.NewAccessToken("schools-sort-created-1"));
        var second = await TestDataFactory.CreateSchoolAsync(db, now, "schools-sort-created-2", TestDataFactory.NewAccessToken("schools-sort-created-2"));

        using var client = await AuthenticatedClientAsync("schools-sort-created-admin");

        var result = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?sort=createdAt", TestJson.Options);

        var ids = result!.Items.Select(s => s.Id).ToList();
        ids.IndexOf(first.Id).Should().BeLessThan(ids.IndexOf(second.Id));
    }
}

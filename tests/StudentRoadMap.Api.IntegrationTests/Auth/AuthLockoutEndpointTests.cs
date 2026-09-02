using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// Blokirovka — `AdminUser.MaxFailedLoginAttempts` (5) ta xato urinishdan keyin 15 daqiqaga
/// bloklanadi (`docs/08` 2-bo'lim, `AdminUser` domen mantig'i P02'da qulflangan — bu yerda
/// faqat AuthController/LoginCommandHandler ORQALI to'g'ri ulanganini tekshiramiz).
/// Real 15-daqiqalik kutish `AdminUserTests`da (`FakeDateTime` bilan) allaqachon qamrab
/// olingan — bu yerda faqat "N-inchi urinishda 423" simsimlanadi.
/// </summary>
public sealed class AuthLockoutEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthLockoutEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_BeshMartaNotogriParol_KetinBlokirovkaQaytaradi()
    {
        const string username = "lockout-user";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);

        using var client = _factory.CreateClient();

        for (var attempt = 1; attempt <= AdminUser.MaxFailedLoginAttempts; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username, password = "NotogriParol1!" },
                TestJson.Options);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"{attempt}-urinish hali blokirovkaga yetmagan");
        }

        // Endi hisob bloklangan — parol to'g'ri bo'lsa ham 423 qaytishi kerak.
        var lockedResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);

        ((int)lockedResponse.StatusCode).Should().Be(423);
        var problem = await lockedResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("ACCOUNT_LOCKED");
    }
}

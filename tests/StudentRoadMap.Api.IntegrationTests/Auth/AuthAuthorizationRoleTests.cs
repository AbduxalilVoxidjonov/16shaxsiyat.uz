using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>
/// `[Authorize(Policy = "SuperAdmin")]` — to'g'ri rol bilan 200, boshqa rol bilan 403
/// (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 8-band: asos policy). MVP'da yagona ishlatiladigan
/// rol `SuperAdmin` (`SchoolAdmin`/`Psychologist` — v2), shu sabab 403'ni ko'rsatish uchun
/// bu yerda ATAYLAB `SchoolAdmin` rolli test foydalanuvchisi yaratiladi (login o'zi rolga
/// qarab cheklanmaydi — faqat keyingi endpoint ruxsati cheklanadi).
/// </summary>
public sealed class AuthAuthorizationRoleTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AuthAuthorizationRoleTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_SuperAdminBolmaganRolBilan_403Qaytaradi()
    {
        const string username = "role-not-superadmin";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username, role: AdminRole.SchoolAdmin);
        }

        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "login rolga qarab cheklanmaydi");
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// QA topilmasi (2026-09-02) — IDOR regressiya qulfi: arxitektura ikkita autentifikatsiya
/// sxemasini qat'iy ajratadi (`docs/08-auth-va-xavfsizlik.md` 2/4-bo'lim) — o'quvchi
/// `X-Session-Token` (`SessionTokenAuthenticationHandler`) va superadmin JWT Bearer
/// (`JwtAuthenticationSetup.SuperAdminPolicy`, `.AddAuthenticationSchemes(JwtBearerDefaults
/// .AuthenticationScheme)` bilan ANIQ shu sxemaga pinlangan). Bu test to'g'ridan-to'g'ri
/// TASDIQLAYDI: kimdir kelajakda `[Authorize]` atributini yoki default sxemani o'zgartirsa,
/// shu test darhol qizil bo'ladi. Alohida `IClassFixture` (rate limiter kvotasi).
/// </summary>
public sealed class AdminIdorRegressionEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminIdorRegressionEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    // `seed` har chaqiruvda UNIKAL bo'lishi shart — bir xil `PublicApiTestFactory` (shu klass
    // ichida) BUTUN sinf umri davomida bitta DB'ni bo'lishadi, shu sabab ikkita test bir xil
    // seed bilan chaqirsa `ux_schools_token`/`ux_schools_slug` unique cheklovi buziladi.
    private async Task<string> StartRealStudentSessionAsync(string seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"idor-student-maktab-{seed}", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, $"IDOR-{seed}".ToUpperInvariant(), 1, questionCount: 1);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Nomozov Iskandar Farhodovich",
            new DateOnly(2010, 3, 3), Gender.Male, 9, "A", "+998901234574", null, null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        return body!.SessionToken;
    }

    private async Task<string> LoginAsAdminAsync(string username)
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await response.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        return login.AccessToken;
    }

    [Fact]
    public async Task OquvchiSessiyaTokeniBilan_AdminSchoolsGaMurojaat_401QaytaradiVaMalumotYoq()
    {
        var sessionToken = await StartRealStudentSessionAsync("schools");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/admin/schools", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "SuperAdminPolicy faqat JWT Bearer sxemasiga pinlangan — SessionToken hisobga olinmaydi");
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("publicUrl").And.NotContain("totalCount", "hech qanday maktab ma'lumoti chiqmasligi kerak");
    }

    [Fact]
    public async Task OquvchiSessiyaTokeniBilan_AdminStudentsGaMurojaat_401QaytaradiVaMalumotYoq()
    {
        var sessionToken = await StartRealStudentSessionAsync("students");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/admin/students", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "SuperAdminPolicy faqat JWT Bearer sxemasiga pinlangan — SessionToken hisobga olinmaydi");
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("fullName").And.NotContain("totalCount", "hech qanday o'quvchi ma'lumoti chiqmasligi kerak");
    }

    [Fact]
    public async Task AdminJwtBilan_OmmaviySessionsMeGaMurojaat_401Qaytaradi()
    {
        var accessToken = await LoginAsAdminAsync("idor-admin-vs-public");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "sessions/me FAQAT SessionToken sxemasiga pinlangan — JWT Bearer hisobga olinmaydi");
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Security;

/// <summary>
/// P47: `X-Session-Token` autentifikatsiyasi endi OCHIQ ustun (`session_token`) bo'yicha emas,
/// SHA-256 XESHI (`assessments.session_token_hash`) bo'yicha qidiradi — refresh tokenlar bilan
/// bir xil himoya darajasi (`docs/08` 4-bo'lim).
/// </summary>
public sealed class SessionTokenHashLookupTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public SessionTokenHashLookupTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> StartSessionAsync(string seed, string fullName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-{seed}", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, seed.ToUpperInvariant(), 1, questionCount: 2);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, new DateOnly(2010, 3, 3),
            Gender.Male, 8, "B", "+998901112233", null, null, true, "uz", TestDataFactory.DefaultProgramCode);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
    }

    [Fact]
    public async Task SessiyaTokeni_XeshUstuniBoyichaTopiladi()
    {
        var sessionToken = await StartSessionAsync("sthash1", "Tursunov Doston Alisherovich");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expectedHash = TokenHash.Compute(sessionToken);

        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.SessionTokenHash == expectedHash);
        assessment.Should().NotBeNull("xesh ustuni domen tomonidan avtomatik to'ldiriladi");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var stateResponse = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        stateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Xesh ustuni buzilsa (yoki bo'shatilsa) autentifikatsiya ISHLAMASLIGI kerak — bu
    /// qidiruv chindan XESH bo'yicha ketayotganini isbotlaydi (ochiq `session_token` ustuni
    /// hali joyida turibdi va u bo'yicha qidirilsa test yashil qolardi).
    /// </summary>
    [Fact]
    public async Task SessiyaTokeni_XeshUstuniOzgartirilgan_401Qaytaradi()
    {
        var sessionToken = await StartSessionAsync("sthash2", "Tursunov Bekzod Alisherovich");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var expectedHash = TokenHash.Compute(sessionToken);

            // Ochiq `session_token` O'ZGARISHSIZ qoladi — faqat xesh buziladi.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE assessments SET session_token_hash = 'buzilgan' WHERE session_token_hash = {expectedHash}");
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

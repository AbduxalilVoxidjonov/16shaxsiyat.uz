using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>Ommaviy (Telegram) oqim testlari uchun yordamchilar.</summary>
internal static class PublicUserTestDataFactory
{
    /// <summary>
    /// Ommaviy makonni (`SchoolKind.PublicSpace`) yaratadi — to'liq `DbSeeder.SeedAsync()`
    /// chaqirmasdan (u 190 savol, tip katalogi va superadminni ham yozadi, testlarning
    /// aksariyatiga kerak emas). `Id`/`Slug`/`Name` seeder bilan AYNAN bir xil konstantalardan
    /// olinadi — ya'ni test seeder bilan bir xil obyektni ko'radi va ikkalasi bir vaqtda
    /// chaqirilsa `ux_schools_public_space` buzilmaydi (mavjud bo'lsa qaytariladi).
    /// </summary>
    public static async Task<School> GetOrCreatePublicSpaceAsync(AppDbContext db, DateTimeOffset now)
    {
        var existing = db.Schools.Local.FirstOrDefault(s => s.Kind == SchoolKind.PublicSpace)
            ?? db.Schools.FirstOrDefault(s => s.Kind == SchoolKind.PublicSpace);

        if (existing is not null)
        {
            return existing;
        }

        var space = School.CreatePublicSpace(
            DbSeeder.PublicSpaceSchoolId,
            DbSeeder.PublicSpaceName,
            SchoolSlug.FromExisting(DbSeeder.PublicSpaceSlug),
            TestDataFactory.NewAccessToken("public-space"),
            now);

        db.Schools.Add(space);
        await db.SaveChangesAsync();

        return space;
    }

    /// <summary>
    /// Telegram orqali kiradi va `(accessToken, refreshCookie, user)` uchligini qaytaradi.
    /// `telegramId` har testda UNIKAL bo'lishi kerak — `IClassFixture` bitta bazani baham
    /// ko'radi va `ux_public_users_telegram` unikal indeksi bor.
    /// </summary>
    public static async Task<(string AccessToken, string RefreshCookie, PublicUserDto User)> LoginAsync(
        HttpClient client,
        long telegramId,
        string? username = null,
        string? firstName = "Ali")
    {
        var payload = TelegramLoginTestSupport.BuildSignedPayload(telegramId, firstName: firstName, username: username);

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Telegram kirishi muvaffaqiyatli bo'lishi kerak");

        var body = await response.Content.ReadFromJsonAsync<TelegramLoginResult>(TestJson.Options);
        body.Should().NotBeNull();

        var cookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_public_refresh_token");
        cookie.Should().NotBeNull("refresh token faqat `httpOnly` cookie'da beriladi");

        return (body!.AccessToken, cookie!, body.User);
    }

    /// <summary>`Authorization: Bearer` sarlavhasi bilan yangi `HttpClient` sozlamasi.</summary>
    public static void UseBearer(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}

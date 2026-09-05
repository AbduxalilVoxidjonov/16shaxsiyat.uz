using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.ConfirmTotp;
using StudentRoadMap.Application.Identity.EnableTotp;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Auth;

/// <summary>TOTP testlari uchun umumiy yordamchilar (seed/login/enable/confirm) — takrorlanishni kamaytiradi.</summary>
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

    /// <summary>O'rnatishning 1-bosqichi. **2FA hali yoqilmaydi** — sir kutish holatiga yoziladi.</summary>
    public static async Task<EnableTotpResult> EnableTotpAsync(HttpClient client, string accessToken)
    {
        var response = await PostAsAdminAsync(client, accessToken, "/api/auth/totp/enable", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<EnableTotpResult>(TestJson.Options))!;
    }

    /// <summary>O'rnatishning 2-bosqichi — 6 xonali kod bilan tasdiqlash (xom javob, status tekshirilmaydi).</summary>
    public static Task<HttpResponseMessage> ConfirmTotpRawAsync(HttpClient client, string accessToken, string code) =>
        PostAsAdminAsync(client, accessToken, "/api/auth/totp/confirm", JsonContent.Create(new { code }, options: TestJson.Options));

    /// <summary>`enable` + `confirm` — 2FA haqiqatdan yoqilgan holatga o'tkazadi va zaxira kodlarni qaytaradi.</summary>
    public static async Task<(EnableTotpResult Enable, ConfirmTotpResult Confirm)> EnrollTotpAsync(HttpClient client, string accessToken)
    {
        var enable = await EnableTotpAsync(client, accessToken);
        var code = TotpTestHelper.ComputeCode(enable.Secret, DateTimeOffset.UtcNow);

        var response = await ConfirmTotpRawAsync(client, accessToken, code);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirm = (await response.Content.ReadFromJsonAsync<ConfirmTotpResult>(TestJson.Options))!;

        return (enable, confirm);
    }

    /// <summary>
    /// Tasdiqlashda ishlatilgan vaqt qadami `TotpLastUsedStep` ga yozilgani uchun (qayta
    /// ishlatishga qarshi himoya) login uchun KEYINGI qadam kodi kerak — aks holda o'sha
    /// qadam rad etiladi.
    /// </summary>
    public static string NextLoginCode(string secret) =>
        TotpTestHelper.ComputeCode(secret, DateTimeOffset.UtcNow.AddSeconds(30));

    /// <summary>
    /// Kutish holatidagi sirning yaratilish vaqtini o'tmishga suradi — muddati o'tgan
    /// o'rnatishni (`TOTP_ENROLLMENT_EXPIRED`) integratsiya sathida sinash uchun yagona yo'l
    /// (test muhitida `IDateTime` almashtirilmaydi).
    /// </summary>
    public static async Task ExpirePendingEnrollmentAsync(PublicApiTestFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.AdminUsers.FirstAsync(u => u.Username == username);

        db.Entry(user).Property(nameof(user.PendingTotpCreatedAt)).CurrentValue =
            DateTimeOffset.UtcNow.AddHours(-1);

        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PostAsAdminAsync(
        HttpClient client,
        string accessToken,
        string path,
        HttpContent? content)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        try
        {
            return await client.PostAsync(new Uri(path, UriKind.Relative), content);
        }
        finally
        {
            client.DefaultRequestHeaders.Authorization = null;
        }
    }
}

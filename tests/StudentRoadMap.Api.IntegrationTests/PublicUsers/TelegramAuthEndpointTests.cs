using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// `POST /api/auth/telegram` — `docs/07` 1a-bo'lim, `docs/08` 2a-bo'lim. Telegram rasmiy
/// imzo algoritmi (`data_check_string` + `HMAC_SHA256(SHA256(bot_token))`) va `auth_date`
/// yangiligi.
/// </summary>
public sealed class TelegramAuthEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private readonly TelegramApiTestFactory _factory;

    public TelegramAuthEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Telegram_ToGriImzoBilan_AkkauntYaratadiVaTokenBeradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(
            700100001, firstName: "Ali", lastName: "Valiyev", username: "alivali", photoUrl: "https://t.me/i/a.jpg");

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TelegramLoginResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().BeGreaterThan(0);
        body.IsNewUser.Should().BeTrue();
        body.User.FirstName.Should().Be("Ali");
        body.User.Username.Should().Be("alivali");

        // Refresh token HECH QACHON javob tanasida bo'lmaydi (`[JsonIgnore]`) — faqat cookie'da.
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("refreshToken");

        var cookie = AdminTestDataFactory.ExtractCookieValue(response, "srm_public_refresh_token");
        cookie.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.PublicUsers.AsNoTracking().SingleAsync(u => u.TelegramId == 700100001);
        user.Id.Should().Be(body.User.Id);

        var auditExists = await db.AuditLogs.AnyAsync(a =>
            a.Action == PublicAuditActions.LoginSucceeded && a.EntityId == user.Id);
        auditExists.Should().BeTrue("`PublicAuth.LoginSucceeded` audit yozuvi qoldirilishi shart");
    }

    [Fact]
    public async Task Telegram_IkkinchiKirish_YangiAkkauntYaratmaydiVaProfilniYangilaydi()
    {
        using var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/api/auth/telegram",
            TelegramLoginTestSupport.BuildSignedPayload(700100002, firstName: "Eski", username: "eski"),
            TestJson.Options);
        first.EnsureSuccessStatusCode();
        var firstBody = (await first.Content.ReadFromJsonAsync<TelegramLoginResult>(TestJson.Options))!;

        var second = await client.PostAsJsonAsync(
            "/api/auth/telegram",
            TelegramLoginTestSupport.BuildSignedPayload(700100002, firstName: "Yangi", username: "yangi"),
            TestJson.Options);
        second.EnsureSuccessStatusCode();
        var secondBody = (await second.Content.ReadFromJsonAsync<TelegramLoginResult>(TestJson.Options))!;

        secondBody.User.Id.Should().Be(firstBody.User.Id, "bir xil `telegram_id` bir xil akkauntga tegishli");
        secondBody.IsNewUser.Should().BeFalse();
        secondBody.User.FirstName.Should().Be("Yangi", "`RecordLogin` profilni har kirishda yangilaydi");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.PublicUsers.CountAsync(u => u.TelegramId == 700100002);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Telegram_OzgartirilganMaydonBilan_401TelegramAuthInvalidQaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(700100003, firstName: "Ali");

        // Imzo hisoblangandan KEYIN maydon o'zgartiriladi — aynan hujum ssenariysi.
        payload["first_name"] = "Boshqa";

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TELEGRAM_AUTH_INVALID");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PublicUsers.AnyAsync(u => u.TelegramId == 700100003)).Should().BeFalse("imzo noto'g'ri bo'lsa akkaunt yaratilmaydi");
        (await db.AuditLogs.AnyAsync(a => a.Action == PublicAuditActions.LoginFailed)).Should().BeTrue();
    }

    [Fact]
    public async Task Telegram_BoshqaBotTokeniBilanImzolangan_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(700100004, botToken: "9999:BOSHQA-BOT-TOKEN");

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TELEGRAM_AUTH_INVALID");
    }

    [Fact]
    public async Task Telegram_AuthDate24SoatdanEski_401TelegramAuthExpiredQaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(
            700100005, authDate: DateTimeOffset.UtcNow.AddHours(-25));

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TELEGRAM_AUTH_EXPIRED");
    }

    [Fact]
    public async Task Telegram_AuthDateKelajakda_401TelegramAuthExpiredQaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(
            700100006, authDate: DateTimeOffset.UtcNow.AddHours(1));

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TELEGRAM_AUTH_EXPIRED");
    }

    [Fact]
    public async Task Telegram_ImzoFormatiNotogri_400ValidationErrorQaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(700100007);
        payload["hash"] = "qisqa";

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }
}

/// <summary>`Telegram:BotToken` berilmagan muhit — `503`, `401` EMAS (`ProblemCodes.TelegramAuthNotConfigured` izohi).</summary>
public sealed class TelegramAuthNotConfiguredEndpointTests : IClassFixture<TelegramNotConfiguredApiTestFactory>
{
    private readonly TelegramNotConfiguredApiTestFactory _factory;

    public TelegramAuthNotConfiguredEndpointTests(TelegramNotConfiguredApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Telegram_BotTokeniSozlanmagan_503Qaytaradi()
    {
        using var client = _factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(700200001);

        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TELEGRAM_AUTH_NOT_CONFIGURED");
    }
}

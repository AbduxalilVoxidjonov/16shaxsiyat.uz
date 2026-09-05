using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Application.PublicUsers.ListAssessments;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// Shaxsiy kabinet — `GET /api/me`, `GET /api/me/assessments`,
/// `GET /api/me/assessments/{id}/result`, `DELETE /api/me` (`docs/07` 5-bo'lim).
/// </summary>
public sealed class MeEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private readonly TelegramApiTestFactory _factory;

    public MeEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private static object SessionBody() => new
    {
        fullName = "Karimov Sardor Alisherovich",
        birthDate = new DateOnly(1995, 4, 12),
        gender = nameof(Gender.Male),
        phone = "+998901234567",
        consentAccepted = true,
        languageCode = "uz",
        programCode = TestDataFactory.DefaultProgramCode,
    };

    private async Task SeedSpaceAndProgramAsync(string testCode, int order)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        await TestDataFactory.CreatePublishedTestAsync(db, now, testCode, order, questionCount: 2);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(long telegramId, string? username = null)
    {
        var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, telegramId, username);
        client.UseBearer(accessToken);
        return client;
    }

    [Fact]
    public async Task Me_ProfilniQaytaradi()
    {
        using var client = await AuthenticatedClientAsync(740100001, username: "kabinet1");

        var response = await client.GetAsync(new Uri("/api/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PublicUserDto>(TestJson.Options);
        body!.Username.Should().Be("kabinet1");
        body.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));

        // Telegram ID mijozga QAYTARILMAYDI (`PublicUserDto` izohi — minimallik prinsipi).
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("telegramId");
    }

    [Fact]
    public async Task Me_TokenSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeAssessments_SessiyaYoq_BoshRoyxatQaytaradi()
    {
        using var client = await AuthenticatedClientAsync(740100002);

        var response = await client.GetAsync(new Uri("/api/me/assessments", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListMyAssessmentsResult>(TestJson.Options);
        body!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task MeAssessments_OchilganSessiyaniDasturNomiBilanQaytaradi()
    {
        await SeedSpaceAndProgramAsync("MEA1", 11);
        using var client = await AuthenticatedClientAsync(740100003);

        var start = await client.PostAsJsonAsync("/api/me/sessions", SessionBody(), TestJson.Options);
        start.StatusCode.Should().Be(HttpStatusCode.Created);
        var started = (await start.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        var response = await client.GetAsync(new Uri("/api/me/assessments", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListMyAssessmentsResult>(TestJson.Options);
        body!.Items.Should().ContainSingle();
        var item = body.Items[0];
        item.Id.Should().Be(started.AssessmentId);
        item.Status.Should().Be("Draft");
        item.ProgramCode.Should().Be(TestDataFactory.DefaultProgramCode);
        item.ProgramName.Should().NotBeNullOrWhiteSpace();
        item.CompletedAt.Should().BeNull();
        item.ResultAvailable.Should().BeFalse("sessiya hali `Analyzed` emas");
    }

    [Fact]
    public async Task MeAssessmentResult_TahlilTayyorEmas_202Qaytaradi()
    {
        await SeedSpaceAndProgramAsync("MEA2", 12);
        using var client = await AuthenticatedClientAsync(740100004);

        var start = await client.PostAsJsonAsync("/api/me/sessions", SessionBody(), TestJson.Options);
        var started = (await start.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        var response = await client.GetAsync(new Uri($"/api/me/assessments/{started.AssessmentId}/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    /// <summary>
    /// `CLAUDE.md` 8-qoidasi bo'yicha ENG MUHIM test: begona sessiya identifikatori bilan
    /// so'ralganda `403` EMAS, `404` qaytishi kerak — aks holda javob "bunday sessiya bor"
    /// ma'lumotini oshkor qilardi (mavjudlik oracle'i).
    /// </summary>
    [Fact]
    public async Task MeAssessmentResult_BoshqaFoydalanuvchiningSessiyasi_404Qaytaradi()
    {
        await SeedSpaceAndProgramAsync("MEA3", 13);

        using var ownerClient = await AuthenticatedClientAsync(740100005);
        var start = await ownerClient.PostAsJsonAsync("/api/me/sessions", SessionBody(), TestJson.Options);
        start.StatusCode.Should().Be(HttpStatusCode.Created);
        var foreignAssessmentId = (await start.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.AssessmentId;

        using var attackerClient = await AuthenticatedClientAsync(740100006);

        var response = await attackerClient.GetAsync(new Uri($"/api/me/assessments/{foreignAssessmentId}/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "mavjudlikni oshkor qilmaslik uchun `403` emas `404`");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task MeAssessmentResult_MavjudBolmaganId_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync(740100007);

        var response = await client.GetAsync(new Uri($"/api/me/assessments/{Guid.NewGuid()}/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>
/// `DELETE /api/me` — o'zini o'chirish. ALOHIDA `IClassFixture`: `PublicTelegramAuth` rate
/// limiti (IP bo'yicha 10/5 daqiqa) bitta test sinfidagi kirishlar sonini cheklaydi, shu
/// sabab kabinet o'qish testlaridan ajratilgan (aks holda sinf o'sganda `429` bilan
/// beqaror bo'lardi).
/// </summary>
public sealed class MeDeleteEndpointTests : IClassFixture<PublicUserDeleteApiTestFactory>
{
    private readonly PublicUserDeleteApiTestFactory _factory;

    public MeDeleteEndpointTests(PublicUserDeleteApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteMe_AkkauntniAnonimlashtiradiVaTokenlarniBekorQiladi()
    {
        using var client = _factory.CreateClient();
        var (accessToken, refreshCookie, user) = await PublicUserTestDataFactory.LoginAsync(client, 740100008, username: "ochiriladi");
        client.UseBearer(accessToken);

        var deleteResponse = await client.DeleteAsync(new Uri("/api/me", UriKind.Relative));

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var stored = await db.PublicUsers.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Id == user.Id);
            stored.DeletedAt.Should().NotBeNull();
            stored.TelegramId.Should().BeNull("anonimlashtirish — Telegram ID tozalanadi");
            stored.Username.Should().BeNull();
            stored.FirstName.Should().BeNull();

            (await db.PublicUsers.AnyAsync(u => u.Id == user.Id)).Should().BeFalse("global filtr o'chirilgan akkauntni yashiradi");

            (await db.PublicRefreshTokens.Where(t => t.PublicUserId == user.Id).AllAsync(t => t.RevokedAt != null))
                .Should().BeTrue("barcha refresh tokenlar bekor qilinadi");

            (await db.AuditLogs.AnyAsync(a => a.Action == PublicAuditActions.AccountDeleted && a.EntityId == user.Id))
                .Should().BeTrue("`PublicUser.Deleted` audit yozuvi qoldirilishi shart");
        }

        // Access token hali "tirik" bo'lsa ham profil ochilmaydi.
        var profileAfterDelete = await client.GetAsync(new Uri("/api/me", UriKind.Relative));
        profileAfterDelete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Refresh token ham ishlamaydi.
        var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/telegram/refresh")
        {
            Headers = { { "Cookie", refreshCookie } },
        };
        (await client.SendAsync(refresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMe_OchirilgandanKeyinQaytaKirish_YangiAkkauntYaratadi()
    {
        using var client = _factory.CreateClient();
        var (accessToken, _, firstUser) = await PublicUserTestDataFactory.LoginAsync(client, 740100009);
        client.UseBearer(accessToken);

        (await client.DeleteAsync(new Uri("/api/me", UriKind.Relative))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var freshClient = _factory.CreateClient();
        var (_, _, secondUser) = await PublicUserTestDataFactory.LoginAsync(freshClient, 740100009);

        secondUser.Id.Should().NotBe(firstUser.Id, "`telegram_id` tozalangani uchun qayta kirish YANGI akkaunt yaratadi");
    }
}

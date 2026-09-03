using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/schools/{slug}` — "havola to'g'ri, lekin test tayyor emas" holati
/// (`docs/07` 1.1, 2026-09-03 jonli hodisasi).
///
/// **Alohida `IClassFixture`** — SHART, tanlov emas: `PublicApiTestFactory` har klass uchun
/// O'Z in-memory bazasini yaratadi, `TestDataFactory.CreatePublishedTestAsync` esa standart
/// `Public` dasturni butun baza uchun NASHR QILADI. Boshqa klassda yashaganida bu yerdagi
/// "dastursiz maktab" holatini umuman qurib bo'lmasdi.
///
/// **Xom JSON ustidan tekshiriladi** (2026-09-03 qarori): `ReadFromJsonAsync&lt;Dto&gt;()`
/// kalit/kod farqini KO'RMAYDI — `code` maydoni aynan `NO_PROGRAM_AVAILABLE` ekanligi shu
/// sabab `JsonElement` orqali o'qiladi.
/// </summary>
public sealed class PublicSchoolInfoNoProgramEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSchoolInfoNoProgramEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    [Fact]
    public async Task GetSchoolInfo_MavjudSlugLekinDastursiz_409NoProgramAvailableQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("noprogram-1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "noprogram-maktab-1", token);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("NO_PROGRAM_AVAILABLE");
        problem.GetProperty("status").GetInt32().Should().Be(409);

        // Ommaviy javobda maktab haqida QO'SHIMCHA ma'lumot oshkor qilinmaydi — faqat holat va xabar.
        var raw = problem.GetRawText();
        raw.Should().NotContain(school.Name);
        raw.Should().NotContain(school.Region);
        raw.Should().NotContain(school.Id.ToString());
    }

    [Fact]
    public async Task GetSchoolInfo_NomalumSlug_HamonNotFoundQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/schools/umuman-mavjud-emas-slug?k=xxx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    /// <summary>
    /// XAVFSIZLIK: yangi kod slug mavjudligini tasdiqlaydi, LEKIN faqat TOKEN to'g'ri bo'lganda.
    /// Tokenni bilmagan kishi hamon `404` oladi — yangi kod unga hech qanday ma'lumot bermaydi.
    /// </summary>
    [Fact]
    public async Task GetSchoolInfo_MavjudSlugLekinNotogriToken_409EmasNotFoundQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("noprogram-token");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "noprogram-maktab-token", token);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            new Uri($"/api/public/schools/{school.Slug.Value}?k=BUTUNLAY-BOSHQA-TOKEN", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    /// <summary>
    /// **Yashirin xavf qulfi.** `409` holatida ham havola ochilishi hisoblagichi OSHISHI kerak:
    /// o'quvchi havolani CHINDAN ochgan, faqat test tayyor emas. Aks holda dastursiz davrdagi
    /// barcha ochilishlar yo'qolib, dashboard voronkasining eng yuqori bo'g'ini
    /// (`school_link_views`) jimgina noto'g'ri bo'lardi.
    /// </summary>
    [Fact]
    public async Task GetSchoolInfo_Dastursiz_409LekinHavolaOchilishiHisoblanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("noprogram-linkview");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "noprogram-maktab-linkview", token);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var views = await db.SchoolLinkViews.Where(v => v.SchoolId == school.Id).SumAsync(v => v.Count);
        views.Should().Be(1, "o'quvchi havolani chindan ochgan — voronkaning eng yuqori bo'g'ini yo'qolmasligi kerak");
    }

}

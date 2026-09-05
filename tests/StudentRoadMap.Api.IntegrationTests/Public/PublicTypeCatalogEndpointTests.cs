using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetTypeCatalog;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/type-catalog` — `docs/07` 1.10-bo'lim. Ommaviy `/metodika` sahifasidagi
/// 16 tip bo'limining manbasi.
///
/// Eng muhim tekshiruv — **autentifikatsiyasiz 200**: bu ochiq marketing kontenti, unga
/// `X-Session-Token` ham, maktab havolasi ham kerak emas. Ikkinchi muhim tekshiruv — JSON
/// maydon nomlari camelCase (loyiha standarti; `npm run generate:api` va frontend shu shaklga
/// tayanadi).
/// </summary>
public sealed class PublicTypeCatalogEndpointTests : IClassFixture<PublicApiTestFactory>
{
    /// <summary>`SeedData/type-catalog.json` dagi kodlar — `DbSeederTests` kutgan 16 ta.</summary>
    private static readonly string[] AllCodes =
    [
        "ISTJ", "ISFJ", "INFJ", "INTJ",
        "ISTP", "ISFP", "INFP", "INTP",
        "ESTP", "ESFP", "ENFP", "ENTP",
        "ESTJ", "ESFJ", "ENFJ", "ENTJ",
    ];

    private readonly PublicApiTestFactory _factory;

    public PublicTypeCatalogEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTypeCatalog_AutentifikatsiyasizChaqiruv_200Va16TaYozuv()
    {
        await SeedTypeCatalogAsync();
        using var client = _factory.CreateClient();

        // Hech qanday sarlavha YO'Q — na `X-Session-Token`, na `Authorization`.
        var response = await client.GetAsync(new Uri("/api/public/type-catalog", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetTypeCatalogResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.Types.Should().HaveCount(16);
        body.Types.Select(t => t.Code).Should().BeEquivalentTo(AllCodes);

        // Har bir yozuvda ommaviy sahifa uchun zarur kontent to'liq bo'lishi kerak.
        body.Types.Should().OnlyContain(t =>
            t.Name.Length > 0
            && t.ShortDescription.Length > 0
            && t.LongDescription.Length > 0
            && t.Strengths.Count > 0
            && t.GrowthAreas.Count > 0
            && t.CareerHints.Count > 0);
    }

    [Fact]
    public async Task GetTypeCatalog_JavobMaydonlariCamelCase()
    {
        await SeedTypeCatalogAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/type-catalog", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var types = json.GetProperty("types");
        types.GetArrayLength().Should().Be(16);

        var first = types[0];
        foreach (var property in new[] { "code", "name", "shortDescription", "longDescription", "strengths", "growthAreas", "careerHints" })
        {
            first.TryGetProperty(property, out _).Should().BeTrue($"`{property}` camelCase nomi bilan qaytishi kerak");
        }

        // PascalCase sizib chiqmasligi kerak — `AddJsonOptions` standartini qo'riqlaydi.
        first.TryGetProperty("Code", out _).Should().BeFalse();
        first.TryGetProperty("ShortDescription", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetTypeCatalog_YaroqsizSessiyaTokeniBilanHam_200Qaytaradi()
    {
        await SeedTypeCatalogAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", "butunlay-yaroqsiz-token");

        var response = await client.GetAsync(new Uri("/api/public/type-catalog", UriKind.Relative));

        // Endpoint sessiyaga BOG'LIQ EMAS — yaroqsiz token 401/410 keltirib chiqarmasligi kerak.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTypeCatalog_JavobdaBallVaShkalaMaydonlariYoq()
    {
        await SeedTypeCatalogAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/type-catalog", UriKind.Relative));
        var raw = await response.Content.ReadAsStringAsync();

        // `CLAUDE.md` 9-qoida — ommaviy javobda shkala/yo'nalish/ball bo'lmaydi.
        foreach (var forbidden in new[] { "\"scale\"", "\"scaleDirection\"", "\"weight\"", "\"score\"" })
        {
            raw.Should().NotContain(forbidden);
        }
    }

    /// <summary>
    /// Integratsiya bazasi `EnsureCreated` bilan BO'SH quriladi (`DbSeeder` bu yerda ishlamaydi),
    /// shu sabab 16 yozuv shu yerda qo'shiladi. Matn o'rinbosar — bu sinov shartnomani
    /// (status, maydon nomlari, yozuvlar soni) tekshiradi, kontentning o'zini emas
    /// (kontent `DbSeederTests` va `type-catalog.json` mas'uliyatida).
    /// </summary>
    private async Task SeedTypeCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.TypeCatalog.AnyAsync())
        {
            return;
        }

        foreach (var code in AllCodes)
        {
            db.TypeCatalog.Add(TypeCatalogEntry.Create(
                code,
                $"{code} nomi",
                $"{code} qisqa tavsifi",
                $"{code} to'liq tavsifi",
                [$"{code} kuchli tomoni"],
                [$"{code} o'sish yo'nalishi"],
                [$"{code} kasb maslahati"]));
        }

        await db.SaveChangesAsync();
    }
}

using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/schools/{slug}?k=` — `school_link_views` hisoblagichi (`prompts/15` vazifa 1,
/// 2026-09-02: "havola ochilishini hisoblash"). Alohida `IClassFixture` (rate limiter kvotasi,
/// `prompts/15` "Yangi test klasslarini alohida IClassFixture'ga qo'y") — `PublicSchoolInfoEndpointTests`
/// bilan bir xil `PublicSchoolInfo` siyosatini (60/daqiqa) baham ko'rmaslik uchun.
/// </summary>
public sealed class PublicSchoolLinkViewEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSchoolLinkViewEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> LinkViewCountAsync(AppDbContext db, Guid schoolId) =>
        await db.SchoolLinkViews.Where(v => v.SchoolId == schoolId).SumAsync(v => v.Count);

    [Fact]
    public async Task GetSchoolInfo_TogriHavolaVaToken_HisoblagichniOshiradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("linkview-ok");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-linkview-ok", token);

        // Bu test HISOBLAGICH haqida, dastur haqida emas — `200` yo'lini saqlab qolish uchun
        // maktabga mavjud dastur beriladi (2026-09-03: dastursiz maktab `409` qaytaradi).
        await TestDataFactory.CreatePublishedTestAsync(db, now, "LINKVIEW_TEST", displayOrder: 1);

        using var client = _factory.CreateClient();

        // Nol — hali hech kim ochmagan.
        (await LinkViewCountAsync(db, school.Id)).Should().Be(0);

        var first = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LinkViewCountAsync(db, school.Id)).Should().Be(1);

        // Ikkinchi ochilish — HAM oshadi (atomik `+1`, `IncrementRegistrationCounterAsync` bilan bir xil naqsh).
        var second = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LinkViewCountAsync(db, school.Id)).Should().Be(2);
    }

    [Fact]
    public async Task GetSchoolInfo_NotogriToken_HisoblagichniOshirmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("linkview-wrongtok");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-linkview-wrongtok", token);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k=BUTUNLAY-BOSHQA-TOKEN", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LinkViewCountAsync(db, school.Id)).Should().Be(0, "noto'g'ri token bilan hisoblagich OSHMASLIGI kerak");
    }

    [Fact]
    public async Task GetSchoolInfo_NofaolMaktab_HisoblagichniOshirmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("linkview-inactive");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-linkview-inactive", token, isActive: false);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        (await LinkViewCountAsync(db, school.Id)).Should().Be(0, "nofaol maktabda hisoblagich OSHMASLIGI kerak");
    }

}

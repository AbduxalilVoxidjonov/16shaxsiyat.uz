using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `hasPersonalityBattery` bayrog'i (`docs/07` 1.1/1.3-bo'lim) — `docs/06` 8-bo'lim, 2026-09-02
/// qaror: dasturda shaxsiyat batareyasi BO'LMASLIGI mumkin. Bayroq `Domain.Catalog.PersonalityBattery`
/// domen qoidasi bilan hisoblanadi (`Kind = Standard` + `ScoringMode = Scored`), metodika KODI
/// bo'yicha qidiruv bilan EMAS.
///
/// Har ssenariy ALOHIDA fixture (`PublicProgramSelectionEndpointTests` bilan bir xil sabab:
/// `Public` ko'rinishdagi dastur barcha maktabda ko'rinadi, bitta fixture'dagi testlar
/// bir-birining dastur katalogiga ta'sir qilardi).
/// </summary>
public sealed class PublicPersonalityBatteryPresentEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicPersonalityBatteryPresentEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Batareyali dastur (seed shaklidagi `Standard`/`Scored` metodika) — `true`.
    /// Metodika kodi ATAYLAB `"MBTI16"` EMAS (`PERS-BAT-1`): eski, satr solishtiruvchi mantiq
    /// aynan shu holatda jimgina `false` berardi.
    /// </summary>
    [Fact]
    public async Task SessionState_BatareyaliDastur_HasPersonalityBatteryTrue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("battery-yes");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battery-yes", accessToken);

        var systemTest = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "PERS-BAT-1", 1, questionCount: 2);
        await TestDataFactory.CreateProgramAsync(db, now, "PROG-BATTERY-YES", [(systemTest.Id, 1)]);

        using var client = _factory.CreateClient();

        var schoolInfo = await client.GetFromJsonAsync<GetSchoolInfoResult>(
            new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative), TestJson.Options);

        schoolInfo!.Programs.Should().ContainSingle(p => p.Code == "PROG-BATTERY-YES")
            .Which.HasPersonalityBattery.Should().BeTrue("dasturda `Standard` + `Scored` ilmiy metodika bor");

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Batareya Bor Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", "+998909998877", null, true, "uz", ProgramCode: "PROG-BATTERY-YES");

        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        client.DefaultRequestHeaders.Add("X-Session-Token", started!.SessionToken);
        var state = await client.GetFromJsonAsync<GetSessionStateResult>(
            new Uri("/api/public/sessions/me", UriKind.Relative), TestJson.Options);

        state!.HasPersonalityBattery.Should().BeTrue();
    }
}

public sealed class PublicPersonalityBatteryAbsentEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicPersonalityBatteryAbsentEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Faqat `Custom` (superadmin yaratgan, ballansa ham `Kind = Custom`) va `Survey` anketali
    /// dastur — `false`. `Custom`/`Scored` blok ataylab `"MBTI16"` KODI bilan yaratilgan: eski,
    /// satr solishtiruvchi mantiq bu holatda xato `true` berardi.
    /// </summary>
    [Fact]
    public async Task SessionState_FaqatCustomVaSurveyAnketa_HasPersonalityBatteryFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("battery-no");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battery-no", accessToken);

        var customScored = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MBTI16", 1, questionCount: 2);
        var survey = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "CAREER-SURVEY-1", 2, questionCount: 2, scoringMode: TestScoringMode.Survey);

        await TestDataFactory.CreateProgramAsync(db, now, "PROG-BATTERY-NO", [(customScored.Id, 1), (survey.Id, 2)]);

        using var client = _factory.CreateClient();

        var schoolInfo = await client.GetFromJsonAsync<GetSchoolInfoResult>(
            new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative), TestJson.Options);

        schoolInfo!.Programs.Should().ContainSingle(p => p.Code == "PROG-BATTERY-NO")
            .Which.HasPersonalityBattery.Should().BeFalse(
                "`MBTI16` KODI bo'lsa ham anketa `Custom` — bayroq kod satriga emas, domen atributlariga tayanadi");

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Batareya Yoq Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", "+998909998877", null, true, "uz", ProgramCode: "PROG-BATTERY-NO");

        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        client.DefaultRequestHeaders.Add("X-Session-Token", started!.SessionToken);
        var state = await client.GetFromJsonAsync<GetSessionStateResult>(
            new Uri("/api/public/sessions/me", UriKind.Relative), TestJson.Options);

        state!.HasPersonalityBattery.Should().BeFalse();
        state.Tests.Should().Contain(t => t.Code == "MBTI16", "ssenariyning ma'nosi aynan shu: kod bor, batareya YO'Q");
    }
}

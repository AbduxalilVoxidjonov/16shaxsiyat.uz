using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>`GET /api/public/schools/{slug}?k=` — `docs/07` 1.1-bo'lim, `prompts/10` DoD.</summary>
public sealed class PublicSchoolInfoEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSchoolInfoEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSchoolInfo_TogriHavola_200VaTestKatalogiQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("valid1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-valid1", token);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "INFO1", 1, questionCount: 3);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSchoolInfoResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.SchoolId.Should().Be(school.Id);
        body.RequiresAccessCode.Should().BeFalse();
        body.Tests.Should().Contain(t => t.Code == "INFO1" && t.QuestionCount == 3);
        body.TotalEstimatedMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSchoolInfo_NotogriToken_404Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("wrongtok1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-wrongtok1", token);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k=BUTUNLAY-BOSHQA-TOKEN", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetSchoolInfo_MavjudEmasSlug_404Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/schools/mavjud-emas-maktab?k=har-qanday-token", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSchoolInfo_NofaolMaktab_410Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("inactive1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-inactive1", token, isActive: false);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SCHOOL_INACTIVE");
    }
}

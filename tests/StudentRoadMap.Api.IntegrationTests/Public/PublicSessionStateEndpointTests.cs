using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>`GET /api/public/sessions/me` — `docs/07` 1.3-bo'lim.</summary>
public sealed class PublicSessionStateEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSessionStateEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> StartNewSessionAsync(HttpClient client, string seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-{seed}", accessToken);

        if (!await db.TestDefinitions.AnyAsync())
        {
            await TestDataFactory.CreatePublishedTestAsync(db, now, "STATE1", 1, questionCount: 2);
        }

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Egamberdiyev Sherzod Anvarovich",
            new DateOnly(2010, 3, 3), Domain.Students.Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    [Fact]
    public async Task GetSessionState_ToGriToken_200VaHolatniQaytaradi()
    {
        using var client = _factory.CreateClient();
        var sessionToken = await StartNewSessionAsync(client, "state1");
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSessionStateResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.Status.Should().Be("Draft");
        body.Student.FirstNameShort.Should().Be("Sherzod");
        body.Tests.Should().ContainSingle(t => t.Code == "STATE1");
        body.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task GetSessionState_TokenBerilmasa_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessionState_NotogriToken_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", "mavjud-emas-token");

        var response = await client.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

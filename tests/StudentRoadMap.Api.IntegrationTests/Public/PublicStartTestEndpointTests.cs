using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.Public.StartTest;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>`POST /api/public/sessions/tests/{testCode}/start` — `docs/07` 1.4-bo'lim, `prompts/11`.</summary>
public sealed class PublicStartTestEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStartTestEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", "+998909998877", null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    [Fact]
    public async Task StartTest_BirinchiTest_InProgressVaSahifalashniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("start1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-start1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "START1", 1, questionCount: 10, pageSize: 4);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Egamov Diyorbek Shuxratovich", new DateOnly(2010, 1, 1));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/START1/start", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StartTestResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.TestCode.Should().Be("START1");
        body.Status.Should().Be("InProgress");
        body.PageSize.Should().Be(4);
        body.TotalPages.Should().Be(3); // ceil(10/4)
    }

    [Fact]
    public async Task StartTest_QaytaChaqirilsa_Idempotent200Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("start2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-start2", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "START2", 1, questionCount: 3);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Nortoshov Sardorbek Muzaffarovich", new DateOnly(2010, 2, 2));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var first = await client.PostAsync(new Uri("/api/public/sessions/tests/START2/start", UriKind.Relative), content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync(new Uri("/api/public/sessions/tests/START2/start", UriKind.Relative), content: null);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<StartTestResult>(TestJson.Options);
        body!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task StartTest_OldingiTestTugallanmagan_409TestNotUnlockedQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("start3");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-start3", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "START3A", 1, questionCount: 2);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "START3B", 2, questionCount: 2);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Qodirova Zilola Baxtiyorovna", new DateOnly(2010, 3, 3));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        // Birinchi testni boshlamasdan to'g'ridan-to'g'ri ikkinchisini boshlashga urinish.
        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/START3B/start", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TEST_NOT_UNLOCKED");
    }

    [Fact]
    public async Task StartTest_NomalumTestKodi_404Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("start4");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-start4", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "START4", 1, questionCount: 2);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Yoqubov Islom Davronovich", new DateOnly(2010, 4, 4));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/NOMAVJUD/start", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartTest_TokenBerilmasa_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/ANY/start", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

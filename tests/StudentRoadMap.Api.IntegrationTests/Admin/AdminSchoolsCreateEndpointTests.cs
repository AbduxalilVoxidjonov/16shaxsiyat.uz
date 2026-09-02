using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>`SchoolsController.Create` — `docs/07` 3.1-bo'lim, `prompts/14` MAXSUS DIQQAT #3. Alohida `IClassFixture`.</summary>
public sealed class AdminSchoolsCreateEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolsCreateEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_ToGriMalumot_201VaSlugAvtomatikGeneratsiyaQilinadi()
    {
        using var client = await AuthenticatedClientAsync("schools-create-admin");

        var request = new
        {
            name = "42-son maktab",
            region = "Farg'ona",
            district = "Qo'qon",
            schoolNumber = (string?)null,
            contactPerson = (string?)null,
            contactPhone = (string?)null,
            accessCode = (string?)null,
            dailyRegistrationLimit = (int?)null,
            notes = (string?)null,
        };

        var response = await client.PostAsJsonAsync("/api/admin/schools", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        body.Slug.Should().Be("42-son-maktab-qoqon");
        body.PublicUrl.Should().Contain(body.Slug).And.Contain("?k=");
        body.IsActive.Should().BeTrue();
        body.Stats.StudentCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_BirXilNomVaTuman_IkkinchiMarta_Slug2BilanYaratiladi()
    {
        using var client = await AuthenticatedClientAsync("schools-create-dup-admin");

        var request = new
        {
            name = "Dublikat maktab",
            region = "Andijon",
            district = "Andijon shahar",
            schoolNumber = (string?)null,
            contactPerson = (string?)null,
            contactPhone = (string?)null,
            accessCode = (string?)null,
            dailyRegistrationLimit = (int?)null,
            notes = (string?)null,
        };

        var first = await client.PostAsJsonAsync("/api/admin/schools", request, TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = (await first.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;

        var second = await client.PostAsJsonAsync("/api/admin/schools", request, TestJson.Options);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondBody = (await second.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;

        secondBody.Slug.Should().Be($"{firstBody.Slug}-2");
        secondBody.Id.Should().NotBe(firstBody.Id);
    }

    [Fact]
    public async Task Create_QandaydirMaydonBoshMas_400ValidationErrorQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("schools-create-invalid-admin");

        var request = new { name = "", region = "Toshkent", district = "Chilonzor" };

        var response = await client.PostAsJsonAsync("/api/admin/schools", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

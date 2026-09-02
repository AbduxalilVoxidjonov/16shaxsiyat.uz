using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>`AssessmentProgramsController` — `prompts/34` E15-band (minimal admin API). Alohida `IClassFixture`.</summary>
public sealed class AdminAssessmentProgramsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentProgramsEndpointTests(PublicApiTestFactory factory)
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
    public async Task Create_ToGriMalumot_201VaDraftCustomQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-create-admin");

        var request = new { code = "CUSTOM-PROG-1", nameUz = "Maxsus dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" };

        var response = await client.PostAsJsonAsync("/api/admin/programs", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.Status.Should().Be("Draft");
        body.Kind.Should().Be("Custom");
        body.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Create_BandKod_409UNIQUE_CONSTRAINT_CONFLICTQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-dupe-admin");
        var request = new { code = "DUPE-PROG-1", nameUz = "Dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" };

        var first = await client.PostAsJsonAsync("/api/admin/programs", request, TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/admin/programs", request, TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("UNIQUE_CONSTRAINT_CONFLICT");
    }

    [Fact]
    public async Task Publish_TestSizDastur_400PROGRAM_NOT_PUBLISHABLEQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-publish-empty-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "EMPTY-PROG-1", nameUz = "Bo'sh dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        var response = await client.PostAsync(new Uri($"/api/admin/programs/{created!.Id}/publish", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_NOT_PUBLISHABLE");
    }

    [Fact]
    public async Task AddTestThenPublish_ToGriOqim_TestBiriktiriladiVaNashrQilinadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var test = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "ADMPROGT1", 1);

        using var client = await AuthenticatedClientAsync("programs-addtest-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "ADD-TEST-PROG-1", nameUz = "Test biriktirish dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        var addTestResponse = await client.PostAsJsonAsync(
            $"/api/admin/programs/{created!.Id}/tests",
            new { testDefinitionId = test.Id, displayOrder = 1 },
            TestJson.Options);
        addTestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAddTest = await addTestResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterAddTest!.Tests.Should().ContainSingle(t => t.TestDefinitionId == test.Id);

        var publishResponse = await client.PostAsync(new Uri($"/api/admin/programs/{created.Id}/publish", UriKind.Relative), content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterPublish = await publishResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterPublish!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task AddTest_TizimDasturigaBiriktirish_409SYSTEM_PROGRAM_LOCKEDQaytaradi()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var systemProgram = await db.AssessmentPrograms.AsNoTracking().SingleAsync(p => p.Code == "PERSONALITY_PROFILE");
        var anyTestDefinitionId = await db.TestDefinitions.AsNoTracking().Select(t => t.Id).FirstAsync();

        using var client = await AuthenticatedClientAsync("programs-system-locked-admin");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/programs/{systemProgram.Id}/tests",
            new { testDefinitionId = anyTestDefinitionId, displayOrder = 99 },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SYSTEM_PROGRAM_LOCKED");
    }

    [Fact]
    public async Task AssignSchoolThenUnassign_ToGriOqim_RoyxatgaQoshiladiVaOlibTashlanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-assign-admin1", TestDataFactory.NewAccessToken("prog-assign-admin1"));

        using var client = await AuthenticatedClientAsync("programs-assign-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "ASSIGN-ADMIN-PROG-1", nameUz = "Biriktirish dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        var assignResponse = await client.PostAsync(new Uri($"/api/admin/programs/{created!.Id}/schools/{school.Id}", UriKind.Relative), content: null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAssign = await assignResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterAssign!.AssignedSchoolIds.Should().Contain(school.Id);

        var unassignResponse = await client.DeleteAsync(new Uri($"/api/admin/programs/{created.Id}/schools/{school.Id}", UriKind.Relative));
        unassignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterUnassign = await unassignResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterUnassign!.AssignedSchoolIds.Should().NotContain(school.Id);
    }

    [Fact]
    public async Task List_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/programs", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_YaratilganDasturniQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-list-admin");
        await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "LIST-PROG-1", nameUz = "Ro'yxat dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);

        var result = await client.GetFromJsonAsync<PagedResult<AdminProgramListItemDto>>("/api/admin/programs?search=LIST-PROG-1", TestJson.Options);

        result!.Items.Should().ContainSingle(p => p.Code == "LIST-PROG-1" && p.Status == "Draft");
    }

    [Fact]
    public async Task ReorderTestsThenRemoveTest_ToGriOqim_TartibOzgaradiVaOlibTashlanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var testA = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "REORDA1", 1);
        var testB = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "REORDB1", 2);

        using var client = await AuthenticatedClientAsync("programs-reorder-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REORDER-PROG-1", nameUz = "Tartiblash dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = testA.Id, displayOrder = 1 }, TestJson.Options);
        await client.PostAsJsonAsync($"/api/admin/programs/{created.Id}/tests", new { testDefinitionId = testB.Id, displayOrder = 2 }, TestJson.Options);

        var reorderResponse = await client.PostAsJsonAsync(
            $"/api/admin/programs/{created.Id}/tests/reorder",
            new { testDefinitionIds = new[] { testB.Id, testA.Id } },
            TestJson.Options);
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterReorder = await reorderResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterReorder!.Tests.Single(t => t.TestDefinitionId == testB.Id).DisplayOrder.Should().Be(1);
        afterReorder.Tests.Single(t => t.TestDefinitionId == testA.Id).DisplayOrder.Should().Be(2);

        var removeResponse = await client.DeleteAsync(new Uri($"/api/admin/programs/{created.Id}/tests/{testA.Id}", UriKind.Relative));
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterRemove = await removeResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterRemove!.Tests.Should().ContainSingle(t => t.TestDefinitionId == testB.Id);
    }
}

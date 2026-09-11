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
        body!.State.Should().Be("Draft");
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
        // Nashr qilingan dastur DARROV `Active` bo'ladi — `Publish` `IsActive`ni ANIQ `true` qiladi.
        afterPublish!.State.Should().Be("Active");
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

    /// <summary>
    /// 2026-09-07: ommaviy makon `school_programs` da oddiy qator, lekin dastur detalida u
    /// "maktab" sifatida chiqmasligi kerak — `AssignedSchoolIds` da YO'Q, o'rniga
    /// `IsAssignedToPublicSpace`. Biriktirish mavjud ommaviy endpoint orqali
    /// (`POST | DELETE /api/admin/public-space/programs/{programId}`), dastur tomonida yangi
    /// endpoint yo'q. Dastur `Draft` — endpoint holatni TEKSHIRMAYDI (`200`), bu fakt shu yerda
    /// qulflanadi: UI "faqat faol dastur" cheklovini o'zi qo'yadi.
    /// </summary>
    [Fact]
    public async Task PublicSpaceAssign_DetalDaAlohidaBayroq_MaktablarRoyxatigaKirmaydi()
    {
        Guid spaceId;
        Guid schoolId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            spaceId = (await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now)).Id;
            schoolId = (await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-public-space", TestDataFactory.NewAccessToken("prog-public-space"))).Id;
        }

        using var client = await AuthenticatedClientAsync("programs-public-space-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "PUBLIC-SPACE-PROG-1", nameUz = "Ommaviy makon dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        created!.IsAssignedToPublicSpace.Should().BeFalse();

        // Oddiy maktab — ro'yxatda qoladi (ajratish faqat ommaviy makonga tegishli).
        var schoolAssign = await client.PostAsync(new Uri($"/api/admin/programs/{created.Id}/schools/{schoolId}", UriKind.Relative), content: null);
        schoolAssign.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignResponse = await client.PostAsync(new Uri($"/api/admin/public-space/programs/{created.Id}", UriKind.Relative), content: null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, "ommaviy endpoint dastur holatini tekshirmaydi — Draft ham biriktiriladi");

        var afterAssign = await client.GetFromJsonAsync<AdminProgramDetailDto>(new Uri($"/api/admin/programs/{created.Id}", UriKind.Relative), TestJson.Options);
        afterAssign!.IsAssignedToPublicSpace.Should().BeTrue();
        afterAssign.AssignedSchoolIds.Should().ContainSingle().Which.Should().Be(schoolId);
        afterAssign.AssignedSchoolIds.Should().NotContain(spaceId, "ommaviy makon maktab emas — u ro'yxatda ko'rinmasligi kerak");

        var unassignResponse = await client.DeleteAsync(new Uri($"/api/admin/public-space/programs/{created.Id}", UriKind.Relative));
        unassignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterUnassign = await client.GetFromJsonAsync<AdminProgramDetailDto>(new Uri($"/api/admin/programs/{created.Id}", UriKind.Relative), TestJson.Options);
        afterUnassign!.IsAssignedToPublicSpace.Should().BeFalse();
        afterUnassign.AssignedSchoolIds.Should().ContainSingle().Which.Should().Be(schoolId);
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

        result!.Items.Should().ContainSingle(p => p.Code == "LIST-PROG-1" && p.State == "Draft");
    }

    /// <summary>
    /// P52 (2026-09-11): `HasPersonalityBattery` — texnik qarz yopilishi. Ilgari admin javobida
    /// bu bayroq YO'Q edi, frontend `"MBTI16"` kabi kod qidirib xulosa chiqarardi
    /// (`PROGRESS.md` risklar jadvali). Mezon — `Domain.Catalog.PersonalityBattery` (`Kind ==
    /// Standard &amp;&amp; ScoringMode == Scored`), kod ro'yxati EMAS: `Custom` test (hatto RIASEC
    /// strategiyasi bilan) bayroqni `true` qilmasligi, faqat haqiqiy `Standard`+`Scored` test
    /// qilishi shu testda tekshiriladi. Ro'yxat (`GET /api/admin/programs`) va batafsil
    /// (`GET /api/admin/programs/{id}`) BIR XIL natija berishi ham shu yerda qulflanadi.
    /// </summary>
    [Fact]
    public async Task AddTest_StandartIlmiyTest_HasPersonalityBatteryToGriHisoblanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var customTest = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "BATTERYCUSTOM1", 1);
        var systemTest = await TestDataFactory.CreateStandaloneSystemTestAsync(db, now, "BATTERYSYSTEM1", 2);

        using var client = await AuthenticatedClientAsync("programs-battery-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "BATTERY-PROG-1", nameUz = "Batareya dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        created!.HasPersonalityBattery.Should().BeFalse("hali hech qanday test biriktirilmagan");

        var addCustomResponse = await client.PostAsJsonAsync(
            $"/api/admin/programs/{created.Id}/tests",
            new { testDefinitionId = customTest.Id, displayOrder = 1 },
            TestJson.Options);
        var afterCustomAdd = await addCustomResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterCustomAdd!.HasPersonalityBattery.Should().BeFalse(
            "RIASEC shaklidagi savollar bilan ham `Custom` test batareyaga kirmaydi — mezon KOD emas");

        var addSystemResponse = await client.PostAsJsonAsync(
            $"/api/admin/programs/{created.Id}/tests",
            new { testDefinitionId = systemTest.Id, displayOrder = 2 },
            TestJson.Options);
        var afterSystemAdd = await addSystemResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        afterSystemAdd!.HasPersonalityBattery.Should().BeTrue("Standard+Scored test biriktirildi");

        var listResult = await client.GetFromJsonAsync<PagedResult<AdminProgramListItemDto>>(
            "/api/admin/programs?search=BATTERY-PROG-1", TestJson.Options);
        listResult!.Items.Should().ContainSingle(p => p.Id == created.Id && p.HasPersonalityBattery);
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

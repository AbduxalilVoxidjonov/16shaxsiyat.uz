using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `GET`/`PUT /api/admin/catalog/tests/{id}/assignment` (2026-09-23 egasi qarori, `docs/07`
/// §3.4.1, `docs/18` §9.7): "Dasturlar" bo'limi o'rniga biriktirish TEST ichida — ichkarida
/// test dasturi (1:1) avtomatik yaratiladi/boshqariladi, ommaviy oqim dastur orqali ishlaydi.
/// </summary>
public sealed class AdminTestAssignmentEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminTestAssignmentEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Tokensiz_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/admin/catalog/tests/{Guid.NewGuid()}/assignment", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_NomaLumTest_404()
    {
        using var client = await AuthenticatedClientAsync("assign-get-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/catalog/tests/{Guid.NewGuid()}/assignment", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_BiriktirilmaganTest_StandartBoshQiymatlar()
    {
        var test = await CreatePublishedTestAsync("ASG-GET-EMPTY");
        using var client = await AuthenticatedClientAsync("assign-get-empty-admin");

        var dto = await client.GetFromJsonAsync<AdminTestAssignmentDto>($"/api/admin/catalog/tests/{test.Id}/assignment", TestJson.Options);

        dto!.TestDefinitionId.Should().Be(test.Id);
        dto.IsConfigured.Should().BeFalse();
        dto.IsPublic.Should().BeFalse();
        dto.SchoolIds.Should().BeEmpty();
        dto.IsInPublicSpace.Should().BeFalse();
        dto.RegistrationMode.Should().Be("Full");
        dto.State.Should().BeNull();
        dto.IsAvailable.Should().BeFalse();
        dto.TestStatus.Should().Be("Published");
        dto.SessionCount.Should().Be(0);
    }

    [Fact]
    public async Task Put_BoshSorov_TestDasturiYaratilmaydi()
    {
        var test = await CreatePublishedTestAsync("ASG-PUT-EMPTY");
        using var client = await AuthenticatedClientAsync("assign-put-empty-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        dto.IsConfigured.Should().BeFalse();
        (await CountTestProgramsAsync(test.Id)).Should().Be(0, "biriktiradigan narsa yo'q — keraksiz dastur yaratilmasin");
    }

    [Fact]
    public async Task Put_IsPublic_BirinchiMartaTestDasturiYaratiladi_VaLandingdaTestNomiKorinadi()
    {
        var test = await CreatePublishedTestAsync("ASG-PUBLIC");
        var (school, accessToken) = await CreateSchoolAsync("asg-public-maktab");
        using var client = await AuthenticatedClientAsync("assign-public-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        dto.IsConfigured.Should().BeTrue();
        dto.IsPublic.Should().BeTrue();
        dto.State.Should().Be("Active", "test nashr qilingan va faol — test dasturi ham ochiq");
        dto.IsAvailable.Should().BeTrue();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var program = await db.AssessmentPrograms.Include(p => p.Tests).SingleAsync(p => p.OwnerTestDefinitionId == test.Id);
            program.Visibility.Should().Be(ProgramVisibility.Public);
            program.Code.Should().Be(test.Code);
            program.NameUz.Should().Be(test.NameUz);
            program.Tests.Should().ContainSingle(t => t.TestDefinitionId == test.Id);

            (await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.CatalogTestAssignmentUpdated && a.EntityId == test.Id))
                .Should().BeTrue();
        }

        // Ommaviy landing: foydalanuvchiga ko'rinadigan dastur nomi = TEST nomi.
        var landing = await GetLandingAsync(school, accessToken);
        var entry = FindProgram(landing, test.Code);
        entry.GetProperty("nameUz").GetString().Should().Be(test.NameUz);
        entry.GetProperty("tests").EnumerateArray().Should().ContainSingle(t => t.GetProperty("code").GetString() == test.Code);

        // Takroriy PUT — ikkinchi dastur yaratilmaydi.
        (await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>() })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CountTestProgramsAsync(test.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Put_Maktablar_ToLiqAlmashtiriladi_OmmaviyMakonAlohidaBoshqariladi()
    {
        var test = await CreatePublishedTestAsync("ASG-SCHOOLS");
        var (schoolA, tokenA) = await CreateSchoolAsync("asg-schools-a");
        var (schoolB, _) = await CreateSchoolAsync("asg-schools-b");
        var (schoolC, _) = await CreateSchoolAsync("asg-schools-c");
        await SeedPublicSpaceAsync();
        using var client = await AuthenticatedClientAsync("assign-schools-admin");

        var first = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { schoolA.Id, schoolB.Id }, isInPublicSpace = true });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDto = (await first.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        firstDto.SchoolIds.Should().BeEquivalentTo(new[] { schoolA.Id, schoolB.Id });
        firstDto.IsInPublicSpace.Should().BeTrue();
        firstDto.IsPublic.Should().BeFalse();

        FindProgram(await GetLandingAsync(schoolA, tokenA), test.Code).GetProperty("nameUz").GetString().Should().Be(test.NameUz);

        // `isInPublicSpace` berilmasa — ommaviy makon biriktirmasi O'ZGARMAYDI.
        var second = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { schoolB.Id, schoolC.Id } });
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDto = (await second.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        secondDto.SchoolIds.Should().BeEquivalentTo(new[] { schoolB.Id, schoolC.Id });
        secondDto.IsInPublicSpace.Should().BeTrue();

        // A maktabdan olib tashlandi — endi A landing'ida bu test yo'q.
        var landingA = await _factory.CreateClient().GetAsync(new Uri($"/api/public/schools/{schoolA.Slug.Value}?k={tokenA}", UriKind.Relative));
        if (landingA.StatusCode == HttpStatusCode.OK)
        {
            var body = await landingA.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("programs").EnumerateArray().Should().NotContain(p => p.GetProperty("code").GetString() == test.Code);
        }
        else
        {
            landingA.StatusCode.Should().Be(HttpStatusCode.Conflict, "A maktabda boshqa dastur bo'lmasa `409 NO_PROGRAM_AVAILABLE`");
        }

        // Hammasini olib tashlash.
        var third = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = Array.Empty<Guid>(), isInPublicSpace = false });
        var thirdDto = (await third.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        thirdDto.SchoolIds.Should().BeEmpty();
        thirdDto.IsInPublicSpace.Should().BeFalse();
        thirdDto.IsConfigured.Should().BeTrue("test dasturi saqlanadi (tarix), faqat biriktirmalar olinadi");
        thirdDto.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Put_DraftTest_RuxsatBeriladi_LekinOchilmaydi()
    {
        var testId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var draft = TestDefinition.Create(testId, "ASG-DRAFT", "Qoralama anketa", 1, estimatedMinutes: 5, scoringStrategyCode: null, now: DateTimeOffset.UtcNow, scoringMode: TestScoringMode.Survey);
            db.TestDefinitions.Add(draft);
            await db.SaveChangesAsync();
        }

        var (school, _) = await CreateSchoolAsync("asg-draft-maktab");
        using var client = await AuthenticatedClientAsync("assign-draft-admin");

        var response = await PutAssignmentAsync(client, testId, new { isPublic = false, schoolIds = new[] { school.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!;
        dto.TestStatus.Should().Be("Draft");
        dto.State.Should().Be("Draft", "test nashr qilinmaguncha test dasturi ham ochilmaydi");
        dto.IsAvailable.Should().BeFalse();
        dto.SchoolIds.Should().Equal(school.Id);
    }

    /// <summary>
    /// Regressiya (code-review, 2026-09-23): oldindan biriktirilgan Draft test nashr qilinganda
    /// test dasturi tarkibsiz yuklanib `Publish` `PROGRAM_NOT_PUBLISHABLE` otardi (400, rollback).
    /// </summary>
    [Fact]
    public async Task DraftTest_MaktabgaBiriktirilib_NashrQilinsa_TestDasturiActiveVaLandingdaKorinadi()
    {
        var testId = await CreateDraftSurveyTestAsync("ASG-DRAFT-PUB");
        var (school, token) = await CreateSchoolAsync("asg-draft-pub-maktab");
        using var client = await AuthenticatedClientAsync("unused");

        (await PutAssignmentAsync(client, testId, new { isPublic = false, schoolIds = new[] { school.Id } }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var publish = await client.PostAsync(new Uri($"/api/admin/catalog/tests/{testId}/publish", UriKind.Relative), null);
        publish.StatusCode.Should().Be(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());

        var assignment = await client.GetFromJsonAsync<AdminTestAssignmentDto>($"/api/admin/catalog/tests/{testId}/assignment", TestJson.Options);
        assignment!.TestStatus.Should().Be("Published");
        assignment.State.Should().Be("Active");
        assignment.IsAvailable.Should().BeTrue();

        FindProgram(await GetLandingAsync(school, token), "ASG-DRAFT-PUB")
            .GetProperty("tests").EnumerateArray().Should().ContainSingle(t => t.GetProperty("code").GetString() == "ASG-DRAFT-PUB");
    }

    [Fact]
    public async Task DraftTest_OmmaviyMakongaBiriktirilib_NashrQilinsa_TestDasturiActive()
    {
        var testId = await CreateDraftSurveyTestAsync("ASG-DRAFT-SPC");
        await SeedPublicSpaceAsync();
        using var client = await AuthenticatedClientAsync("unused");

        var assign = await client.PostAsync(new Uri($"/api/admin/public-space/tests/{testId}", UriKind.Relative), null);
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
        (await assign.Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options))!.Programs
            .Should().ContainSingle(p => p.TestDefinitionId == testId).Which.State.Should().Be("Draft");

        var publish = await client.PostAsync(new Uri($"/api/admin/catalog/tests/{testId}/publish", UriKind.Relative), null);
        publish.StatusCode.Should().Be(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());

        var space = await client.GetFromJsonAsync<AdminPublicSpaceDto>("/api/admin/public-space", TestJson.Options);
        var entry = space!.Programs.Should().ContainSingle(p => p.TestDefinitionId == testId).Subject;
        entry.State.Should().Be("Active");
        entry.HasUsableTest.Should().BeTrue();
    }

    [Fact]
    public async Task TestFaolligiOzgarsa_TestDasturiHolatiErgashadi()
    {
        var test = await CreatePublishedTestAsync("ASG-TOGGLE");
        using var client = await AuthenticatedClientAsync("assign-toggle-admin");
        (await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>() })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsync(new Uri($"/api/admin/catalog/tests/{test.Id}/toggle-active", UriKind.Relative), null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var paused = await client.GetFromJsonAsync<AdminTestAssignmentDto>($"/api/admin/catalog/tests/{test.Id}/assignment", TestJson.Options);
        paused!.State.Should().Be("Paused");
        paused.IsAvailable.Should().BeFalse();

        (await client.PostAsync(new Uri($"/api/admin/catalog/tests/{test.Id}/toggle-active", UriKind.Relative), null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var active = await client.GetFromJsonAsync<AdminTestAssignmentDto>($"/api/admin/catalog/tests/{test.Id}/assignment", TestJson.Options);
        active!.State.Should().Be("Active");
        active.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task TestNomiOzgarsa_LandingdaYangiNomKorinadi()
    {
        var test = await CreatePublishedTestAsync("ASG-RENAME");
        var (school, token) = await CreateSchoolAsync("asg-rename-maktab");
        using var client = await AuthenticatedClientAsync("assign-rename-admin");
        (await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { school.Id } })).StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync(
            $"/api/admin/catalog/tests/{test.Id}",
            new { nameUz = "Yangi nom (ASG)", descriptionUz = "Yangi tavsif", displayOrder = 5, estimatedMinutes = 5, shuffleQuestions = false, pageSize = 10 },
            TestJson.Options);
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = FindProgram(await GetLandingAsync(school, token), test.Code);
        entry.GetProperty("nameUz").GetString().Should().Be("Yangi nom (ASG)");
        entry.GetProperty("descriptionUz").GetString().Should().Be("Yangi tavsif");
    }

    [Fact]
    public async Task Put_ArxivlanganTest_409TestArchived()
    {
        var test = await CreatePublishedTestAsync("ASG-ARCHIVED");
        using var client = await AuthenticatedClientAsync("assign-archived-admin");
        (await client.PostAsync(new Uri($"/api/admin/catalog/tests/{test.Id}/archive", UriKind.Relative), null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be("TEST_ARCHIVED");
    }

    [Fact]
    public async Task Put_NomaLumMaktab_404()
    {
        var test = await CreatePublishedTestAsync("ASG-NOSCHOOL");
        using var client = await AuthenticatedClientAsync("assign-noschool-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { Guid.NewGuid() } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadCodeAsync(response)).Should().Be("NOT_FOUND");
        (await CountTestProgramsAsync(test.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Put_OmmaviyMakonIdsiMaktabSifatida_404()
    {
        var test = await CreatePublishedTestAsync("ASG-SPACEID");
        var space = await SeedPublicSpaceAsync();
        using var client = await AuthenticatedClientAsync("assign-spaceid-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { space.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "ommaviy makon `schoolIds` orqali emas, `isInPublicSpace` bilan boshqariladi");
    }

    [Fact]
    public async Task Put_NotogriRegistrationMode_400()
    {
        var test = await CreatePublishedTestAsync("ASG-BADMODE");
        using var client = await AuthenticatedClientAsync("assign-badmode-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>(), registrationMode = "Partial" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(response)).Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Put_RegistrationModeNone_OddiyTestdaSaqlanadi()
    {
        var test = await CreatePublishedTestAsync("ASG-NONE", TestScoringMode.Survey);
        using var client = await AuthenticatedClientAsync("assign-none-admin");

        var response = await PutAssignmentAsync(client, test.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>(), registrationMode = "None" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!.RegistrationMode.Should().Be("None");
    }

    [Fact]
    public async Task Put_BatareyaliTestdaRegistrationModeNone_400()
    {
        TestDefinition battery;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            battery = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, DateTimeOffset.UtcNow, "ASG-MBTI", 1);
        }

        using var client = await AuthenticatedClientAsync("assign-battery-admin");

        var response = await PutAssignmentAsync(client, battery.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>(), registrationMode = "None" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(response)).Should().Be("REGISTRATION_REQUIRED_FOR_BATTERY");
        (await CountTestProgramsAsync(battery.Id)).Should().Be(0, "xato bo'lsa tranzaksiya qaytariladi");

        var ok = await PutAssignmentAsync(client, battery.Id, new { isPublic = true, schoolIds = Array.Empty<Guid>() });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ok.Content.ReadFromJsonAsync<AdminTestAssignmentDto>(TestJson.Options))!.HasPersonalityBattery.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTest_OzTestDasturiBorSessiyasiz_TestVaDasturOchiriladi()
    {
        var test = await CreatePublishedTestAsync("ASG-DELETE");
        var (school, _) = await CreateSchoolAsync("asg-delete-maktab");
        using var client = await AuthenticatedClientAsync("assign-delete-admin");
        (await PutAssignmentAsync(client, test.Id, new { isPublic = false, schoolIds = new[] { school.Id } })).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.DeleteAsync(new Uri($"/api/admin/catalog/tests/{test.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CountTestProgramsAsync(test.Id)).Should().Be(0);
    }

    [Fact]
    public async Task OmmaviyMakon_TestBilanBiriktirishVaOlibTashlash()
    {
        var test = await CreatePublishedTestAsync("ASG-SPACE");
        await SeedPublicSpaceAsync();
        using var client = await AuthenticatedClientAsync("assign-space-admin");

        var assign = await client.PostAsync(new Uri($"/api/admin/public-space/tests/{test.Id}", UriKind.Relative), null);
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
        var space = (await assign.Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options))!;
        var entry = space.Programs.Should().ContainSingle(p => p.TestDefinitionId == test.Id).Subject;
        entry.NameUz.Should().Be(test.NameUz);
        entry.State.Should().Be("Active");

        var assignment = await client.GetFromJsonAsync<AdminTestAssignmentDto>($"/api/admin/catalog/tests/{test.Id}/assignment", TestJson.Options);
        assignment!.IsInPublicSpace.Should().BeTrue();
        assignment.IsPublic.Should().BeFalse();

        var unassign = await client.DeleteAsync(new Uri($"/api/admin/public-space/tests/{test.Id}", UriKind.Relative));
        unassign.StatusCode.Should().Be(HttpStatusCode.OK);
        (await unassign.Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options))!.Programs
            .Should().NotContain(p => p.TestDefinitionId == test.Id);
    }

    [Fact]
    public async Task EskiDasturlarEndpointi_EndiYoq()
    {
        using var client = await AuthenticatedClientAsync("assign-legacy-admin");

        var response = await client.GetAsync(new Uri("/api/admin/programs", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------------------

    private async Task<TestDefinition> CreatePublishedTestAsync(string code, TestScoringMode scoringMode = TestScoringMode.Scored)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await TestDataFactory.CreateStandaloneTestAsync(db, DateTimeOffset.UtcNow, code, displayOrder: 1, scoringMode: scoringMode);
    }

    /// <summary>Nashrga TAYYOR, lekin hali `Draft` so'rovnoma (`Survey`, Likert savollar).</summary>
    private async Task<Guid> CreateDraftSurveyTestAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var testId = Guid.NewGuid();

        var test = TestDefinition.Create(testId, code, $"{code} nomi", 1, estimatedMinutes: 5, scoringStrategyCode: null, now: now, scoringMode: TestScoringMode.Survey);
        for (var i = 1; i <= 2; i++)
        {
            test.AddQuestion(
                Question.Create(Guid.NewGuid(), testId, $"{code}-Q{i:00}", i, $"{code} savoli {i}", QuestionType.Likert5, "GEN", scaleDirection: 1, weight: 1.0m, isRequired: true),
                now);
        }

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return testId;
    }

    private async Task<(School School, string AccessToken)> CreateSchoolAsync(string slugSeed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accessToken = TestDataFactory.NewAccessToken(slugSeed);

        var school = await TestDataFactory.CreateSchoolAsync(db, DateTimeOffset.UtcNow, slugSeed, accessToken);
        return (school, accessToken);
    }

    private async Task<School> SeedPublicSpaceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, DateTimeOffset.UtcNow);
    }

    private async Task<int> CountTestProgramsAsync(Guid testId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.AssessmentPrograms.CountAsync(p => p.OwnerTestDefinitionId == testId);
    }

    private static Task<HttpResponseMessage> PutAssignmentAsync(HttpClient client, Guid testId, object body) =>
        client.PutAsJsonAsync($"/api/admin/catalog/tests/{testId}/assignment", body, TestJson.Options);

    private async Task<JsonElement> GetLandingAsync(School school, string accessToken)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static JsonElement FindProgram(JsonElement landing, string code) =>
        landing.GetProperty("programs").EnumerateArray().Should()
            .ContainSingle(p => p.GetProperty("code").GetString() == code).Subject;

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }

    /// <summary>
    /// Login rate limiti (`RateLimitSetup`) sinf ichida ko'p test uchun bitta token ishlatishni
    /// talab qiladi — token fikstura (factory) bo'yicha bir marta olinadi.
    /// </summary>
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static (PublicApiTestFactory Factory, string Token)? _cachedToken;

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        _ = username;
        var token = await GetTokenAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> GetTokenAsync()
    {
        await TokenLock.WaitAsync();
        try
        {
            if (_cachedToken is { } cached && ReferenceEquals(cached.Factory, _factory))
            {
                return cached.Token;
            }

            const string adminUsername = "test-assignment-admin";
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, adminUsername);
            }

            using var client = _factory.CreateClient();
            var loginResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = adminUsername, password = AdminTestDataFactory.DefaultPassword },
                TestJson.Options);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

            _cachedToken = (_factory, login.AccessToken);
            return login.AccessToken;
        }
        finally
        {
            TokenLock.Release();
        }
    }
}

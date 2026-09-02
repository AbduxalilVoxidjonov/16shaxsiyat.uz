using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// Dastur tanlash oqimi — `docs/06` 8-bo'lim (2026-09-02 qaror), `prompts/34` DoD. Har bir
/// stsenariy ALOHIDA `IClassFixture` (`CLAUDE.md` talabi) — `Public` ko'rinishdagi dastur BARCHA
/// maktabda ko'rinadi, shu sabab bitta sinf ichidagi bir nechta fact bir-birining dastur
/// katalogiga (shared SQLite baza) ta'sir qilib qo'yishi mumkin edi.
/// </summary>
public sealed class PublicSingleProgramAutoSelectEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSingleProgramAutoSelectEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// ⚠️ MAJBURIY REGRESSIYA TESTI (`prompts/34` DoD): maktabga faqat bitta dastur
    /// biriktirilgan bo'lsa, o'quvchi oqimi AVVALGIDEK ishlaydi — tanlov ekranisiz,
    /// `programCode`siz. `TestDataFactory.CreatePublishedTestAsync` yaratgan test avtomatik
    /// standart dasturga qo'shiladi — shu fixture'da faqat bitta dastur mavjud bo'ladi.
    /// </summary>
    [Fact]
    public async Task StartSession_BittaDasturBiriktirilgan_ProgramCodeSizAvtomatikTanlanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("prog-single1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-single1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "SNGL1", 1, questionCount: 2);

        using var client = _factory.CreateClient();

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Regressiya Talabasi Ismoilovich", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", null, null, true, "uz", ProgramCode: null);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "bitta dastur bo'lsa tanlov ekranisiz avtomatik ishlashi shart");
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        body!.Tests.Should().ContainSingle(t => t.Code == "SNGL1");
    }
}

public sealed class PublicMultiProgramRequiredEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicMultiProgramRequiredEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_IkkiDasturBiriktirilgan_ProgramCodeSiz400PROGRAM_REQUIREDQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("prog-multi1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-multi1", accessToken);

        var testA = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MULTIA1", 1, questionCount: 2);
        var testB = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MULTIB1", 1, questionCount: 2);
        await TestDataFactory.CreateProgramAsync(db, now, "MULTI-PROG-A1", [(testA.Id, 1)]);
        await TestDataFactory.CreateProgramAsync(db, now, "MULTI-PROG-B1", [(testB.Id, 1)]);

        using var client = _factory.CreateClient();

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Ikki Dastur Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", null, null, true, "uz", ProgramCode: null);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_REQUIRED");
    }

    [Fact]
    public async Task StartSession_IkkiDasturBiriktirilgan_ToGriProgramCodeBilanOSessiyaniOchadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("prog-multi2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-multi2", accessToken);

        var testA = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MULTIA2", 1, questionCount: 2);
        var testB = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MULTIB2", 1, questionCount: 2);
        await TestDataFactory.CreateProgramAsync(db, now, "MULTI-PROG-A2", [(testA.Id, 1)]);
        await TestDataFactory.CreateProgramAsync(db, now, "MULTI-PROG-B2", [(testB.Id, 1)]);

        using var client = _factory.CreateClient();

        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Togri Dastur Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", null, null, true, "uz", ProgramCode: "MULTI-PROG-B2");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        body!.Tests.Should().ContainSingle(t => t.Code == "MULTIB2");
    }
}

public sealed class PublicProgramWrongSchoolEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicProgramWrongSchoolEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_BoshqaMaktabgaBiriktirilganDastur_404Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessTokenOwner = TestDataFactory.NewAccessToken("prog-owner1");
        var accessTokenOther = TestDataFactory.NewAccessToken("prog-other1");
        var ownerSchool = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-owner1", accessTokenOwner);
        var otherSchool = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-other1", accessTokenOther);

        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, "ASSIGNED1", 1, questionCount: 2);
        var program = await TestDataFactory.CreateProgramAsync(db, now, "ASSIGNED-PROG1", [(test.Id, 1)], ProgramVisibility.Assigned);

        // Faqat `ownerSchool`ga biriktirilgan — `otherSchool` uchun mavjud emas.
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), ownerSchool.Id, program.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        var command = new StartSessionCommand(
            otherSchool.Slug.Value, accessTokenOther, null, "Notogri Maktab Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", null, null, true, "uz", ProgramCode: "ASSIGNED-PROG1");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "boshqa maktabga biriktirilgan dasturning mavjudligi oshkor qilinmaydi");
    }
}

public sealed class PublicProgramAssignedOkEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicProgramAssignedOkEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_AssignedDastur_BiriktirilganMaktabdaIshlaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("prog-assigned-ok1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-assigned-ok1", accessToken);

        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, "ASSIGNEDOK1", 1, questionCount: 2);
        var program = await TestDataFactory.CreateProgramAsync(db, now, "ASSIGNED-PROG-OK1", [(test.Id, 1)], ProgramVisibility.Assigned);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        // Faqat shu bitta (Assigned) dastur mavjud bo'lgani uchun `programCode`siz ham ishlashi kerak.
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Biriktirilgan Dastur Talabasi", new DateOnly(2010, 5, 5),
            Gender.Male, 9, "A", "+998901234567", null, null, true, "uz", ProgramCode: null);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        body!.Tests.Should().ContainSingle(t => t.Code == "ASSIGNEDOK1");
    }
}

public sealed class PublicGetSchoolInfoProgramsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicGetSchoolInfoProgramsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSchoolInfo_DasturlarRoyxatiniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("prog-info1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-prog-info1", accessToken);
        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, "INFOPROG1", 1, questionCount: 3);
        await TestDataFactory.CreateProgramAsync(db, now, "INFO-PROG1", [(test.Id, 1)]);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSchoolInfoResult>(TestJson.Options);
        body!.Programs.Should().ContainSingle(p => p.Code == "INFO-PROG1" && p.TestCount == 1 && p.QuestionCount == 3 && p.EstimatedMinutes > 0);
    }
}

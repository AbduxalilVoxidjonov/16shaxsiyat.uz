using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Settings;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasidagi superadmin qo'shgan "o'z maydonlari" (`customFields`) —
/// maktab oqimi (`POST /api/public/sessions`), P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2).
/// Alohida `IClassFixture` — `PublicStartSession` rate limiter kvotasi (10/soat/IP) boshqa
/// test klasslari bilan bo'linmasligi uchun.
/// </summary>
public sealed class PublicRegistrationCustomFieldsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private const string ShortTextCode = "PARENT_JOB";
    private const string SingleChoiceCode = "TRANSPORT_MODE";
    private const string MultiChoiceCode = "HOBBIES";

    private readonly PublicApiTestFactory _factory;

    public PublicRegistrationCustomFieldsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static RegistrationFormDefinition BuildDefinition(RegistrationFieldRequirement shortTextRequirement) =>
        RegistrationFormDefinition.Create(
            RegistrationFormDefinition.Default.CoreFields,
            [
                new RegistrationCustomField(
                    ShortTextCode, QuestionType.ShortText, "Ota-onangiz kasbi", null,
                    shortTextRequirement, MaxLength: 20, InputPattern: null, Options: null, Order: 9),
                new RegistrationCustomField(
                    SingleChoiceCode, QuestionType.SingleChoice, "Transport turi", null,
                    RegistrationFieldRequirement.Optional, MaxLength: null, InputPattern: null,
                    Options: [new RegistrationCustomFieldOption("Piyoda", "walk", 1), new RegistrationCustomFieldOption("Avtobus", "bus", 2)],
                    Order: 10),
                new RegistrationCustomField(
                    MultiChoiceCode, QuestionType.MultiChoice, "Qiziqishlar", null,
                    RegistrationFieldRequirement.Optional, MaxLength: null, InputPattern: null,
                    Options: [new RegistrationCustomFieldOption("Sport", "sport", 1), new RegistrationCustomFieldOption("Musiqa", "music", 2), new RegistrationCustomFieldOption("Kitob", "books", 3)],
                    Order: 11),
            ]);

    private async Task<(School School, string AccessToken, string ProgramCode)> SeedAsync(string seed, RegistrationFieldRequirement shortTextRequirement)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        // Upsert — `IClassFixture` bitta bazani BUTUN klass bo'yicha baham ko'radi (metod
        // bo'yicha EMAS), shu sabab bir nechta test metodi bir xil singleton qatorni yozadi.
        var existingSettings = await db.RegistrationFormSettings.FirstOrDefaultAsync(s => s.Id == RegistrationFormSettings.SingletonId);
        if (existingSettings is null)
        {
            db.RegistrationFormSettings.Add(RegistrationFormSettings.Create(BuildDefinition(shortTextRequirement), now, updatedByAdminUserId: null));
        }
        else
        {
            existingSettings.UpdateDefinition(BuildDefinition(shortTextRequirement), now, updatedByAdminUserId: null);
        }

        var accessToken = TestDataFactory.NewAccessToken($"rcf{seed}");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-rcf{seed}", accessToken);
        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, $"RCF{seed}", 1, questionCount: 1);

        var program = AssessmentProgram.Create(
            Guid.NewGuid(), $"RCF-PROG-{seed}", "O'z maydonlari sinovi", now, visibility: ProgramVisibility.Assigned);
        program.AddTest(test.Id, 1, isPersonalityBatteryTest: false, now);
        program.Publish(now, hasPersonalityBattery: false);

        db.AssessmentPrograms.Add(program);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        return (school, accessToken, program.Code);
    }

    private static object BaseBody(School school, string accessToken, string programCode, string fullName, string phone, object? customFields = null) => new
    {
        slug = school.Slug.Value,
        accessToken,
        fullName,
        birthDate = "2010-05-05",
        gender = "Male",
        grade = 8,
        phone,
        consentAccepted = true,
        languageCode = "uz",
        programCode,
        customFields,
    };

    [Fact]
    public async Task StartSession_RequiredOzMaydoniBoshBoLsa400()
    {
        var (school, accessToken, programCode) = await SeedAsync("req1", RegistrationFieldRequirement.Required);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/public/sessions",
            BaseBody(school, accessToken, programCode, "Aliyev Vali", "+998901111101"),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty(ShortTextCode, out _).Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_HiddenOzMaydoni_KelganQiymatSaqlanmaydi()
    {
        var (school, accessToken, programCode) = await SeedAsync("hid1", RegistrationFieldRequirement.Hidden);
        using var client = _factory.CreateClient();

        var body = BaseBody(
            school, accessToken, programCode, "Ergasheva Zulfiya", "+998901111102",
            customFields: new Dictionary<string, object> { [ShortTextCode] = "Duxtir" });

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.ProfileExtra.Should().BeNull("`Hidden` maydon uchun kelgan qiymat e'tiborsiz qoldirilishi — saqlanmasligi kerak");
    }

    [Fact]
    public async Task StartSession_TurBoYichaValidatsiya_HarUchTuriToGriSaqlanadi()
    {
        var (school, accessToken, programCode) = await SeedAsync("ok1", RegistrationFieldRequirement.Optional);
        using var client = _factory.CreateClient();

        var body = BaseBody(
            school, accessToken, programCode, "Sattorov Jamshid", "+998901111103",
            customFields: new Dictionary<string, object>
            {
                [ShortTextCode] = "O'qituvchi",
                [SingleChoiceCode] = 2,
                [MultiChoiceCode] = new[] { 1, 3 },
            });

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.ProfileExtra.Should().NotBeNull();
        var extra = JsonDocument.Parse(student.ProfileExtra!).RootElement;
        extra.GetProperty(ShortTextCode).GetString().Should().Be("O'qituvchi");
        extra.GetProperty(SingleChoiceCode).GetInt32().Should().Be(2);
        extra.GetProperty(MultiChoiceCode).EnumerateArray().Select(e => e.GetInt32()).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task StartSession_SingleChoiceNotoGriQiymat_400()
    {
        var (school, accessToken, programCode) = await SeedAsync("bad1", RegistrationFieldRequirement.Optional);
        using var client = _factory.CreateClient();

        var body = BaseBody(
            school, accessToken, programCode, "Nosirova Feruza", "+998901111104",
            customFields: new Dictionary<string, object> { [SingleChoiceCode] = 99 });

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty(SingleChoiceCode, out _).Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_MultiChoiceTakroriyQiymat_400()
    {
        var (school, accessToken, programCode) = await SeedAsync("bad2", RegistrationFieldRequirement.Optional);
        using var client = _factory.CreateClient();

        var body = BaseBody(
            school, accessToken, programCode, "Qodirov Sanjar", "+998901111105",
            customFields: new Dictionary<string, object> { [MultiChoiceCode] = new[] { 1, 1 } });

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty(MultiChoiceCode, out _).Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_ShortTextMaxLengthOshsa_400()
    {
        var (school, accessToken, programCode) = await SeedAsync("bad3", RegistrationFieldRequirement.Optional);
        using var client = _factory.CreateClient();

        var body = BaseBody(
            school, accessToken, programCode, "Xoliqova Sevinch", "+998901111106",
            customFields: new Dictionary<string, object> { [ShortTextCode] = new string('a', 25) });

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty(ShortTextCode, out _).Should().BeTrue();
    }
}

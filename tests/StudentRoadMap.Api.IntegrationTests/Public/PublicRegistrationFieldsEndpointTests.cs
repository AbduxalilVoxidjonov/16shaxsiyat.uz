using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// P52 kengaytmasi (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9.5, egasining qarori):
/// `AssessmentProgram.RegistrationFields` — har bir ro'yxatdan o'tish maydonining holati
/// (`Hidden`/`Optional`/`Required`) HAR DASTURDA alohida sozlanadi. Alohida `IClassFixture` —
/// `PublicStartSession` rate limiter kvotasi (10/soat/IP) boshqa test klasslari bilan
/// bo'linmasligi uchun.
/// </summary>
public sealed class PublicRegistrationFieldsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicRegistrationFieldsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Bitta maktab, bitta `Full` rejimli, moslashtirilgan `RegistrationFields` bilan dastur (batareyasiz).</summary>
    private async Task<(School School, string AccessToken, string ProgramCode)> SeedCustomFieldsProgramAsync(
        string seed, RegistrationFields fields)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken($"rf{seed}");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-rf{seed}", accessToken);

        // `CreateStandaloneTestAsync` — `Custom` + `Scored`, ya'ni shaxsiyat batareyasiga
        // KIRMAYDI (`PublicRegistrationModeEndpointTests`dagi bilan bir xil sabab).
        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, $"RF{seed}", 1, questionCount: 1);

        var program = AssessmentProgram.Create(
            Guid.NewGuid(), $"RF-PROG-{seed}", "Moslashtirilgan ro'yxatdan o'tish (sinov)", now,
            visibility: ProgramVisibility.Assigned, registrationFields: fields);
        program.AddTest(test.Id, 1, isPersonalityBatteryTest: false, now);
        program.Publish(now, hasPersonalityBattery: false);

        db.AssessmentPrograms.Add(program);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        return (school, accessToken, program.Code);
    }

    [Fact]
    public async Task StartSession_HiddenPhone_KelganQiymatSaqlanmaydi()
    {
        var fields = RegistrationFields.Default with { Phone = RegistrationFieldRequirement.Hidden };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("hp1", fields);
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Yusupov Jasur",
            birthDate = "2010-05-05",
            gender = "Male",
            grade = 8,
            phone = "+998901234567",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "phone `Hidden` bo'lsa ham majburiy emas — sessiya ochiladi");
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Phone.Should().BeNull("`Hidden` maydon uchun kelgan qiymat e'tiborsiz qoldirilishi — saqlanmasligi kerak");
    }

    [Fact]
    public async Task StartSession_OptionalBirthDate_BoSHQoldirilsaSessionOchiladiVaTakrorlanishTekshiruviBajarilmaydi()
    {
        var fields = RegistrationFields.Default with { BirthDate = RegistrationFieldRequirement.Optional };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("ob1", fields);
        using var client = _factory.CreateClient();

        object Body() => new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Karimova Nodira",
            gender = "Female",
            grade = 7,
            phone = "+998901234568",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var first = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);
        var second = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created, "`birthDate` yo'q bo'lsa BR-1 takrorlanish tekshiruvi bajarilmasligi kerak");

        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstAssessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == firstBody.AssessmentId);
        var secondAssessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == secondBody.AssessmentId);

        firstAssessment.StudentId.Should().NotBe(secondAssessment.StudentId, "identifikator ishonchsiz bo'lgani uchun HAR DOIM yangi o'quvchi yaratilishi kerak");

        var firstStudent = await db.Students.AsNoTracking().SingleAsync(s => s.Id == firstAssessment.StudentId);
        firstStudent.IsAnonymous.Should().BeFalse("F.I.Sh. bor — bu anonim oqim EMAS, faqat `birthDate` ixtiyoriy");
        firstStudent.BirthDate.Should().BeNull();
    }

    [Fact]
    public async Task StartSession_RequiredEmail_BoshBoSa400()
    {
        var fields = RegistrationFields.Default with { Email = RegistrationFieldRequirement.Required };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("re1", fields);
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Rustamov Sherzod",
            birthDate = "2011-02-02",
            grade = 6,
            phone = "+998901234569",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_RequiredGender_BoshBoSa400()
    {
        var fields = RegistrationFields.Default with { Gender = RegistrationFieldRequirement.Required };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("rg1", fields);
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Toshpulatov Bekzod",
            birthDate = "2012-03-03",
            grade = 5,
            phone = "+998901234570",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("gender", out _).Should().BeTrue();
    }

    /// <summary>`gender: Optional` sozlansa jinssiz sessiya muvaffaqiyatli ochiladi (P52 tuzatishi, 2026-09-11).</summary>
    [Fact]
    public async Task StartSession_OptionalGender_JinssizMuvaffaqiyatliBoLadi()
    {
        var fields = RegistrationFields.Default with { Gender = RegistrationFieldRequirement.Optional };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("og1", fields);
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Nazarova Malika",
            birthDate = "2012-04-04",
            grade = 5,
            phone = "+998901234571",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "`gender` `Optional` bo'lsa bo'sh qoldirish mumkin");
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Gender.Should().Be(Gender.Unspecified);
    }

    /// <summary>`gender: Hidden` bo'lsa kelgan qiymat e'tiborsiz qoldiriladi — `Unspecified` saqlanadi.</summary>
    [Fact]
    public async Task StartSession_HiddenGender_KelganQiymatSaqlanmaydi()
    {
        var fields = RegistrationFields.Default with { Gender = RegistrationFieldRequirement.Hidden };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("hg1", fields);
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Egamberdiyev Otabek",
            birthDate = "2012-05-05",
            gender = "Male",
            grade = 5,
            phone = "+998901234572",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "`gender` `Hidden` bo'lsa ham majburiy emas — sessiya ochiladi");
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Gender.Should().Be(Gender.Unspecified, "`Hidden` maydon uchun kelgan qiymat e'tiborsiz qoldirilishi — saqlanmasligi kerak");
    }

    /// <summary>`GET /api/public/schools/{slug}` — `programs[].registrationFields` shakli (`docs/07` §1.1).</summary>
    [Fact]
    public async Task GetSchoolInfo_ProgramsRoyxatida_RegistrationFieldsQaytaradi()
    {
        var fields = RegistrationFields.Default with
        {
            Phone = RegistrationFieldRequirement.Hidden,
            Email = RegistrationFieldRequirement.Required,
        };
        var (school, accessToken, programCode) = await SeedCustomFieldsProgramAsync("gsi1", fields);
        using var client = _factory.CreateClient();

        var info = await client.GetFromJsonAsync<GetSchoolInfoResult>(
            $"/api/public/schools/{school.Slug.Value}?k={accessToken}", TestJson.Options);

        var program = info!.Programs.Should().ContainSingle(p => p.Code == programCode).Which;
        program.RegistrationFields.BirthDate.Should().Be("Required");
        program.RegistrationFields.Gender.Should().Be("Required");
        program.RegistrationFields.Grade.Should().Be("Required");
        program.RegistrationFields.ClassLetter.Should().Be("Optional");
        program.RegistrationFields.Phone.Should().Be("Hidden");
        program.RegistrationFields.ParentPhone.Should().Be("Optional");
        program.RegistrationFields.Email.Should().Be("Required");
    }

    /// <summary>
    /// Regressiya qulfi: standart sozlama (`RegistrationFields` dasturda `null`, ya'ni sozlanmagan)
    /// bilan xatti-harakat — `fullName`/`phone`/`gender` yo'q bo'lsa `400`, boshqa maydonlar
    /// (`classLetter`/`parentPhone`/`email`) haqida xato YO'Q. `gender` 2026-09-11 kuni standart
    /// bo'yicha `Required`ga o'zgardi (`RegistrationFields.cs` izohiga qarang — ommaviy forma
    /// jinsni ALLAQACHON majburiy qilardi, backend endi shu xatti-harakatga moslashtirildi).
    /// </summary>
    [Fact]
    public async Task StartSession_StandartSozlama_FishTelefonVaJinssiz400VaBoshqaXatoYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("rfdefault1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-rfdefault1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "RFDEFAULT1", 1, questionCount: 1);

        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            birthDate = "2010-01-01",
            grade = 9,
            consentAccepted = true,
            languageCode = "uz",
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("fullName", out _).Should().BeTrue();
        errors.TryGetProperty("phone", out _).Should().BeTrue();
        errors.TryGetProperty("gender", out _).Should().BeTrue();
        errors.TryGetProperty("classLetter", out _).Should().BeFalse();
        errors.TryGetProperty("parentPhone", out _).Should().BeFalse();
        errors.TryGetProperty("email", out _).Should().BeFalse();
    }
}

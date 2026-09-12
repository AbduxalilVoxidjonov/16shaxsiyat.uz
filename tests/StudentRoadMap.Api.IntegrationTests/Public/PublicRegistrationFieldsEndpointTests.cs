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
using StudentRoadMap.Domain.Settings;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2, egasining qarori): ro'yxatdan o'tish
/// maydonlarining holati (`Hidden`/`Optional`/`Required`) endi HAR DASTURDA alohida EMAS —
/// GLOBAL `RegistrationFormSettings` orqali boshqariladi (`AssessmentProgram.RegistrationFields`,
/// §9.5, ENDI O'QILMAYDI). Bu fayl ilgari (§9.5) dastur darajasidagi sozlamani sinardi — endi
/// AYNAN SHU xatti-harakatni GLOBAL sozlama orqali sinaydi. Alohida `IClassFixture` —
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

    /// <summary>`RegistrationFormDefinition.Default`ning bitta yoki bir nechta `coreFields` maydonini almashtiradi — qolgani standart.</summary>
    private static RegistrationFormDefinition DefaultDefinitionWith(
        RegistrationFieldRequirement? birthDate = null,
        RegistrationFieldRequirement? gender = null,
        RegistrationFieldRequirement? grade = null,
        RegistrationFieldRequirement? classLetter = null,
        RegistrationFieldRequirement? phone = null,
        RegistrationFieldRequirement? parentPhone = null,
        RegistrationFieldRequirement? email = null)
    {
        var core = RegistrationFormDefinition.Default.CoreFields;
        var newCore = core with
        {
            BirthDate = core.BirthDate with { Requirement = birthDate ?? core.BirthDate.Requirement },
            Gender = core.Gender with { Requirement = gender ?? core.Gender.Requirement },
            Grade = core.Grade with { Requirement = grade ?? core.Grade.Requirement },
            ClassLetter = core.ClassLetter with { Requirement = classLetter ?? core.ClassLetter.Requirement },
            Phone = core.Phone with { Requirement = phone ?? core.Phone.Requirement },
            ParentPhone = core.ParentPhone with { Requirement = parentPhone ?? core.ParentPhone.Requirement },
            Email = core.Email with { Requirement = email ?? core.Email.Requirement },
        };

        return RegistrationFormDefinition.Create(newCore, []);
    }

    /// <summary>
    /// Upsert — singleton qator (`RegistrationFormSettings.SingletonId`). `IClassFixture` bir
    /// baza/klass umriga tegishli (metod bo'yicha EMAS), shu sabab bir nechta test metodi bir
    /// xil qatorni yozadi — `Add` ikkinchi chaqiruvda `UNIQUE` xatosiga olib kelardi.
    /// </summary>
    private static async Task SeedGlobalRegistrationFormAsync(AppDbContext db, DateTimeOffset now, RegistrationFormDefinition definition)
    {
        var existing = await db.RegistrationFormSettings.FirstOrDefaultAsync(s => s.Id == RegistrationFormSettings.SingletonId);
        if (existing is null)
        {
            db.RegistrationFormSettings.Add(RegistrationFormSettings.Create(definition, now, updatedByAdminUserId: null));
        }
        else
        {
            existing.UpdateDefinition(definition, now, updatedByAdminUserId: null);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Bitta maktab, bitta `Full` rejimli dastur (batareyasiz — GLOBAL sozlama ustunlik olmaydi).</summary>
    private async Task<(School School, string AccessToken, string ProgramCode)> SeedProgramAsync(string seed)
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
            visibility: ProgramVisibility.Assigned);
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
        var (school, accessToken, programCode) = await SeedProgramAsync("hp1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(phone: RegistrationFieldRequirement.Hidden));
        }

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

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db2.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Phone.Should().BeNull("`Hidden` maydon uchun kelgan qiymat e'tiborsiz qoldirilishi — saqlanmasligi kerak");
    }

    [Fact]
    public async Task StartSession_OptionalBirthDate_TelefonBoLsaOquvchiTopiladiVaSessiyaTiklanadi()
    {
        // P52 kod ko'rigi tuzatmasi (2026-09-11). Ilgari bu test TESKARISINI qulflardi:
        // `birthDate` yo'q bo'lsa qidiruv butunlay o'chirilib, HAR DOIM yangi o'quvchi
        // yaratilardi. Bu xato edi — o'sha qidiruv sessiyani TIKLASH (BR-5) uchun ham
        // ishlatiladi, ya'ni o'quvchi qaytib kelganda yarim qolgan testi yo'qolardi.
        // Endi kalit sifatida F.I.Sh. + TELEFON ishlatiladi (bir xil ism va bir xil telefon —
        // amalda bitta odam).
        var (school, accessToken, programCode) = await SeedProgramAsync("ob1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(birthDate: RegistrationFieldRequirement.Optional));
        }

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
        // Tiklangan sessiya `200` qaytaradi, `201` EMAS — yangi resurs yaratilmagan
        // (`docs/07` §1.2 `resumed: true` shartnomasi).
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        secondBody.Resumed.Should().BeTrue("yarim qolgan sessiya tiklanishi kerak, noldan boshlanmasligi");
        secondBody.AssessmentId.Should().Be(firstBody.AssessmentId);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == firstBody.AssessmentId);
        var student = await db2.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.IsAnonymous.Should().BeFalse("F.I.Sh. bor — bu anonim oqim EMAS, faqat `birthDate` ixtiyoriy");
        student.BirthDate.Should().BeNull();
        (await db2.Students.AsNoTracking().CountAsync(s => s.SchoolId == school.Id)).Should().Be(1, "ikkinchi so'rov yangi o'quvchi yaratmasligi kerak");
    }

    [Fact]
    public async Task StartSession_BirthDateVaTelefonYoQ_HarSafarYangiOquvchiYaratiladi()
    {
        // Uchinchi holat: hech qanday ishonchli kalit yo'q (`birthDate` ixtiyoriy va
        // kiritilmagan, telefon esa `Hidden`). Faqat ism bo'yicha izlash bir xil ismli ikki
        // o'quvchini bitta yozuvga qo'shib yuborardi — ma'lumot buzilishi takroriy yozuvdan
        // yomonroq, shu sabab qidiruv ataylab bajarilmaydi.
        var (school, accessToken, programCode) = await SeedProgramAsync("ob2");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(
                db,
                DateTimeOffset.UtcNow,
                DefaultDefinitionWith(birthDate: RegistrationFieldRequirement.Optional, phone: RegistrationFieldRequirement.Hidden));
        }

        using var client = _factory.CreateClient();

        object Body() => new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Karimova Nodira",
            gender = "Female",
            grade = 7,
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var first = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);
        var second = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstAssessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == firstBody.AssessmentId);
        var secondAssessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == secondBody.AssessmentId);

        firstAssessment.StudentId.Should().NotBe(secondAssessment.StudentId, "ishonchli kalit yo'q — har doim yangi o'quvchi");
    }

    [Fact]
    public async Task StartSession_RequiredEmail_BoshBoSa400()
    {
        var (school, accessToken, programCode) = await SeedProgramAsync("re1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(email: RegistrationFieldRequirement.Required));
        }

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

    /// <summary>`parentPhone` standart bo'yicha ixtiyoriy — GLOBAL sozlamada `Required` qilinsa bo'sh qoldirib bo'lmaydi.</summary>
    [Fact]
    public async Task StartSession_RequiredParentPhone_BoshBoSa400()
    {
        var (school, accessToken, programCode) = await SeedProgramAsync("rpp1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(parentPhone: RegistrationFieldRequirement.Required));
        }

        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Toshpulatov Bekzod",
            birthDate = "2012-03-03",
            gender = "Male",
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
        problem.GetProperty("errors").TryGetProperty("parentPhone", out _).Should().BeTrue();
    }

    /// <summary>`gender: Optional` sozlansa jinssiz sessiya muvaffaqiyatli ochiladi (P52 tuzatishi, 2026-09-11).</summary>
    [Fact]
    public async Task StartSession_OptionalGender_JinssizMuvaffaqiyatliBoLadi()
    {
        var (school, accessToken, programCode) = await SeedProgramAsync("og1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(gender: RegistrationFieldRequirement.Optional));
        }

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

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db2.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Gender.Should().Be(Gender.Unspecified);
    }

    /// <summary>`gender: Hidden` bo'lsa kelgan qiymat e'tiborsiz qoldiriladi — `Unspecified` saqlanadi.</summary>
    [Fact]
    public async Task StartSession_HiddenGender_KelganQiymatSaqlanmaydi()
    {
        var (school, accessToken, programCode) = await SeedProgramAsync("hg1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(db, DateTimeOffset.UtcNow, DefaultDefinitionWith(gender: RegistrationFieldRequirement.Hidden));
        }

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

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db2.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db2.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.Gender.Should().Be(Gender.Unspecified, "`Hidden` maydon uchun kelgan qiymat e'tiborsiz qoldirilishi — saqlanmasligi kerak");
    }

    /// <summary>`GET /api/public/schools/{slug}` — `programs[].registrationFields` shakli (`docs/07` §1.1), endi GLOBAL sozlamadan.</summary>
    [Fact]
    public async Task GetSchoolInfo_ProgramsRoyxatida_RegistrationFieldsQaytaradi()
    {
        var (school, accessToken, programCode) = await SeedProgramAsync("gsi1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedGlobalRegistrationFormAsync(
                db,
                DateTimeOffset.UtcNow,
                DefaultDefinitionWith(phone: RegistrationFieldRequirement.Hidden, email: RegistrationFieldRequirement.Required));
        }

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

        // `registrationForm` — TO'LIQ GLOBAL ta'rif (`docs/18` §9.6.2), BIR XIL manbadan.
        program.RegistrationForm.CoreFields.Phone.Requirement.Should().Be("Hidden");
        program.RegistrationForm.CoreFields.Email.Requirement.Should().Be("Required");
        program.RegistrationForm.CustomFields.Should().BeEmpty();
    }

    /// <summary>
    /// Regressiya qulfi: standart sozlama bilan xatti-harakat — `fullName`/`phone`/`gender`
    /// yo'q bo'lsa `400`, boshqa maydonlar (`classLetter`/`parentPhone`/`email`) haqida xato
    /// YO'Q. Sozlama ANIQ `Default`ga o'rnatiladi (`IClassFixture` bitta bazani BUTUN klass
    /// bo'yicha baham ko'radi — boshqa test metodlari qatorni allaqachon o'zgartirgan bo'lishi
    /// mumkin, "qator yo'q = standart" holatiga tayanish tartibga bog'liq bo'lib qolardi).
    /// </summary>
    [Fact]
    public async Task StartSession_StandartSozlama_FishTelefonVaJinssiz400VaBoshqaXatoYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        await SeedGlobalRegistrationFormAsync(db, now, RegistrationFormDefinition.Default);
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

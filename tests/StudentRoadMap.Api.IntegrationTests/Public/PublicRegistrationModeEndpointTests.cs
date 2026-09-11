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
/// P52 (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9, egasining 2026-09-11 qarori):
/// `AssessmentProgram.RegistrationMode.None` — registratsiya ekrani ko'rsatilmagan dastur,
/// `Student` ANONIM yaratiladi. Alohida `IClassFixture` — `PublicStartSession` rate limiter
/// kvotasi (10/soat/IP) boshqa test klasslari bilan bo'linmasligi uchun.
/// </summary>
public sealed class PublicRegistrationModeEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicRegistrationModeEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Bitta maktab, bitta `None` rejimli (`Public`, boshqa dastursiz) dastur — avtomatik tanlanadi.</summary>
    private async Task<(School School, string AccessToken, string ProgramCode)> SeedNoneModeProgramAsync(string seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken($"rm{seed}");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-rm{seed}", accessToken);

        // `CreateStandaloneTestAsync` — `Custom` + `Scored`, ya'ni shaxsiyat batareyasiga
        // KIRMAYDI (`PersonalityBattery.Includes` — `Kind == Standard` talab qiladi) — `None`
        // rejimi bilan mos keladigan yagona holat.
        var test = await TestDataFactory.CreateStandaloneTestAsync(db, now, $"RM{seed}", 1, questionCount: 1);

        // `Assigned` (`Public` EMAS) — `PublicStartSessionPerProgramEndpointTests` izohidagi
        // bilan bir xil sabab: `Public` dastur BARCHA maktabda ko'rinadi, ya'ni bir
        // `IClassFixture` ichidagi boshqa testlar seed qilgan dastur bu maktabga ham
        // "sizib" kirib, "bir nechta dastur mavjud" (`PROGRAM_REQUIRED`) holatini keltirib
        // chiqarardi.
        var program = AssessmentProgram.Create(
            Guid.NewGuid(), $"RM-PROG-{seed}", "Ro'yxatdan o'tishsiz dastur (sinov)", now,
            visibility: ProgramVisibility.Assigned, registrationMode: RegistrationMode.None);
        program.AddTest(test.Id, 1, now);
        program.Publish(now, hasPersonalityBattery: false);

        db.AssessmentPrograms.Add(program);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        return (school, accessToken, program.Code);
    }

    // `programCode` HAR DOIM ANIQ beriladi — bir fixture (`IClassFixture`) ichidagi boshqa
    // testlar `TestDataFactory.CreatePublishedTestAsync` orqali umumiy `Public` "standart
    // dastur"ni (`TestDataFactory.DefaultProgramCode`) yaratishi mumkin; shunda maktabda
    // BIR NECHTA mavjud dastur bo'lib qoladi va `programCode`siz so'rov `400 PROGRAM_REQUIRED`
    // qaytarardi (`PublicStartSessionPerProgramEndpointTests`dagi bilan bir xil sabab).
    private static object AnonymousBody(School school, string accessToken, string programCode, bool consentAccepted = true) => new
    {
        slug = school.Slug.Value,
        accessToken,
        consentAccepted,
        languageCode = "uz",
        programCode,
    };

    [Fact]
    public async Task StartSession_RegistrationModeNone_ShaxsMaydonlarisiz201VaAnonimOquvchiYaratadi()
    {
        var (school, accessToken, programCode) = await SeedNoneModeProgramAsync("1");
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/sessions", AnonymousBody(school, accessToken, programCode), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        body.Resumed.Should().BeFalse();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == body.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.IsAnonymous.Should().BeTrue();
        student.BirthDate.Should().BeNull();
        student.Phone.Should().BeNull();
        student.FullName.Should().StartWith("Anonim ishtirokchi #");
    }

    /// <summary>Kelgan shaxs maydonlari ham E'TIBORSIZ qoldiriladi — dastur `None` bo'lsa ular hech qachon saqlanmaydi.</summary>
    [Fact]
    public async Task StartSession_RegistrationModeNone_ShaxsMaydonlariKelsaHamEtiborsizQoldiriladi()
    {
        var (school, accessToken, programCode) = await SeedNoneModeProgramAsync("2");
        using var client = _factory.CreateClient();

        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Berkitilgan Ism Familiya",
            birthDate = "2010-01-01",
            gender = "Male",
            grade = 9,
            phone = "+998901234567",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.IsAnonymous.Should().BeTrue();
        student.FullName.Should().NotBe("Berkitilgan Ism Familiya");
        student.BirthDate.Should().BeNull();
        student.Phone.Should().BeNull();
    }

    /// <summary>`ConsentAccepted` — huquqiy rozilik, `None` rejimida HAM majburiy.</summary>
    [Fact]
    public async Task StartSession_RegistrationModeNone_ConsentAcceptedSiz400()
    {
        var (school, accessToken, programCode) = await SeedNoneModeProgramAsync("3");
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/sessions", AnonymousBody(school, accessToken, programCode, consentAccepted: false), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("consentAccepted", out _).Should().BeTrue();
    }

    /// <summary>
    /// BR-1 (90 kunlik takror topshirish tekshiruvi) anonim oqimda identifikator yo'qligi
    /// sabab ISHLAMAYDI — bir xil "brauzer" (bu yerda: bir xil so'rov tanasi) ikki marta
    /// yangi anonim sessiya ocha oladi (qabul qilingan cheklov, egasi bilib turib tanladi).
    /// </summary>
    [Fact]
    public async Task StartSession_RegistrationModeNone_IkkalaSoRovYangiAnonimOquvchiYaratadi()
    {
        var (school, accessToken, programCode) = await SeedNoneModeProgramAsync("4");
        using var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/public/sessions", AnonymousBody(school, accessToken, programCode), TestJson.Options);
        var second = await client.PostAsJsonAsync("/api/public/sessions", AnonymousBody(school, accessToken, programCode), TestJson.Options);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created, "BR-1 anonim oqimda ishlamaydi — 409 emas");

        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        firstBody.AssessmentId.Should().NotBe(secondBody.AssessmentId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstAssessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == firstBody.AssessmentId);
        var secondAssessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == secondBody.AssessmentId);
        firstAssessment.StudentId.Should().NotBe(secondAssessment.StudentId, "har so'rov YANGI o'quvchi yaratadi");
    }

    /// <summary>`GET /api/public/schools/{slug}` — `programs[].registrationMode` shakli (`docs/07` §1.1).</summary>
    [Fact]
    public async Task GetSchoolInfo_ProgramsRoyxatida_RegistrationModeQaytaradi()
    {
        var (school, accessToken, programCode) = await SeedNoneModeProgramAsync("5");
        using var client = _factory.CreateClient();

        var info = await client.GetFromJsonAsync<GetSchoolInfoResult>(
            $"/api/public/schools/{school.Slug.Value}?k={accessToken}", TestJson.Options);

        var program = info!.Programs.Should().ContainSingle(p => p.Code == programCode).Which;
        program.RegistrationMode.Should().Be("None");
    }

    /// <summary>
    /// Regressiya qulfi: `RegistrationMode.Full` dasturda (default dastur) shaxs maydonlari
    /// HAMON majburiy — validator o'zgarishi (`When`-shartli) buni bo'shatib qo'ymasligi kerak.
    /// </summary>
    [Fact]
    public async Task StartSession_RegistrationModeFull_FishVaTelefonsiz400()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("rmfull1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-rmfull1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "RMFULL1", 1, questionCount: 1);

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
    }
}

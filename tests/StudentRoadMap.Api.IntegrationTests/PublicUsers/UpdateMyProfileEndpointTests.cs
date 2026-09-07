using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.GetStudentProfile;
using StudentRoadMap.Application.PublicUsers.ListAssessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>`UpdateMyProfileEndpointTests` uchun alohida host (`POST /api/me/sessions` kvotasi bo'linmasin).</summary>
public sealed class PublicUserProfileUpdateApiTestFactory : TelegramApiTestFactory
{
}

/// <summary>
/// `PUT /api/me/profile` (`docs/07` §5.1b) — anketani FAQAT saqlash. Asosiy tasdiq: har qanday
/// holatda `Assessment` YARATILMAYDI (egasi ko'rgan xato: "O'zgartirish" test boshlab yuborardi).
/// Majburiylik qoidalari `POST /api/me/sessions` bilan BIR XIL — xato shakli ham.
/// </summary>
public sealed class UpdateMyProfileEndpointTests : IClassFixture<PublicUserProfileUpdateApiTestFactory>
{
    private readonly PublicUserProfileUpdateApiTestFactory _factory;

    public UpdateMyProfileEndpointTests(PublicUserProfileUpdateApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>`PUT` tanasi — `languageCode`/`programCode` YO'Q (sessiyaga tegishli).</summary>
    private static object ProfileBody(
        string fullName = "Karimov Sardor Alisherovich",
        int birthYear = 1995,
        int? grade = null,
        bool consentAccepted = true,
        bool? parentalConsent = null) => new
        {
            fullName,
            birthDate = new DateOnly(birthYear, 4, 12),
            gender = nameof(Gender.Male),
            phone = "+998901234567",
            consentAccepted,
            parentalConsent,
            grade,
        };

    private async Task SeedSpaceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    private async Task<Student> LoadStudentAsync(long telegramId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.PublicUsers.AsNoTracking().SingleAsync(u => u.TelegramId == telegramId);
        return await db.Students.AsNoTracking().SingleAsync(s => s.PublicUserId == user.Id);
    }

    private async Task<int> CountAssessmentsAsync(Guid studentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Assessments.AsNoTracking().CountAsync(a => a.StudentId == studentId);
    }

    [Fact]
    public async Task UpdateProfile_ProfilYoq_YaratadiVaSessiyaOchmaydi()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100001, lastName: "Valiyev");

        var response = await client.PutAsJsonAsync("/api/me/profile", ProfileBody(grade: 9), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        dto.HasProfile.Should().BeTrue();
        dto.FullName.Should().Be("Karimov Sardor Alisherovich");
        dto.BirthDate.Should().Be(new DateOnly(1995, 4, 12));
        dto.Gender.Should().Be(Gender.Male);
        dto.Phone.Should().Be("+998901234567");
        dto.Grade.Should().Be(9);
        dto.ConsentVersion.Should().Be("1.0");
        dto.ConsentCurrent.Should().BeTrue();
        dto.SuggestedFullName.Should().Be("Valiyev Ali", "javob `GET /api/me/profile` bilan bir xil shaklda");

        var student = await LoadStudentAsync(760100001);
        student.PublicUserId.Should().NotBeNull();
        student.Grade.Should().Be(9);
        (await CountAssessmentsAsync(student.Id)).Should().Be(0, "profil saqlash sessiya OCHMAYDI — asosiy tasdiq");

        var history = await client.GetFromJsonAsync<ListMyAssessmentsResult>("/api/me/assessments", TestJson.Options);
        history!.Items.Should().BeEmpty();

        // `GET` ham aynan shu profilni qaytaradi.
        var fetched = await client.GetFromJsonAsync<MyStudentProfileDto>("/api/me/profile", TestJson.Options);
        fetched.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task UpdateProfile_ProfilYoq_BoshTana_400BarchaMajburiyMaydonlar_SessiyaBilanBirXilShakl()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100002);

        var response = await client.PutAsJsonAsync("/api/me/profile", new { }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("fullName", "birthDate", "gender", "phone", "consentAccepted");
    }

    [Fact]
    public async Task UpdateProfile_ProfilYoq_VoyagaYetmagan_OtaOnaRoziligiSiz_400ParentalConsent()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100003);
        var minorBirthYear = DateTime.UtcNow.Year - 15;

        var response = await client.PutAsJsonAsync(
            "/api/me/profile",
            ProfileBody(birthYear: minorBirthYear, grade: 9, parentalConsent: false),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("parentalConsent");
    }

    [Fact]
    public async Task UpdateProfile_ProfilBor_KelganMaydonlarTahrir_YuborilmaganiOzgarmaydi()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100004);

        var created = await client.PutAsJsonAsync("/api/me/profile", ProfileBody(grade: 9), TestJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var consentGivenAtBefore = (await LoadStudentAsync(760100004)).ConsentGivenAt;

        // "O'zgartirish": F.I.Sh., telefon, email yangilanadi; `grade: 0` — "maktabda o'qimayman";
        // tug'ilgan sana va jins YUBORILMAYDI → o'zgarmaydi. Rozilik joriy — isbot sanasi tegilmaydi.
        var edit = await client.PutAsJsonAsync(
            "/api/me/profile",
            new
            {
                fullName = "Karimova Malika Alisherovna",
                phone = "+998911112233",
                email = "malika@example.com",
                grade = 0,
            },
            TestJson.Options);

        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await edit.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        dto.FullName.Should().Be("Karimova Malika Alisherovna");
        dto.Phone.Should().Be("+998911112233");
        dto.Email.Should().Be("malika@example.com");
        dto.Grade.Should().BeNull("`0` — sinf yo'q, mijozga `null`");
        dto.BirthDate.Should().Be(new DateOnly(1995, 4, 12));

        var student = await LoadStudentAsync(760100004);
        student.NormalizedName.Should().Be("KARIMOVA MALIKA ALISHEROVNA");
        student.Grade.Should().Be(Student.NoGrade);
        student.Gender.Should().Be(Gender.Male);
        student.ConsentGivenAt.Should().Be(consentGivenAtBefore, "rozilik joriy — isbot sanasi qayta yozilmaydi");
        (await CountAssessmentsAsync(student.Id)).Should().Be(0, "tahrir ham sessiya ochmaydi");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Students.CountAsync(s => s.PublicUserId == student.PublicUserId)).Should().Be(1, "bitta akkaunt → bitta profil");
    }

    [Fact]
    public async Task UpdateProfile_ProfilBor_BoshTana_200HechNarsaOzgarmaydi()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100005);
        (await client.PutAsJsonAsync("/api/me/profile", ProfileBody(), TestJson.Options)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PutAsJsonAsync("/api/me/profile", new { }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "profil bor va rozilik joriy — hech narsa talab qilinmaydi");
        var dto = (await response.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        dto.FullName.Should().Be("Karimov Sardor Alisherovich");
    }

    [Fact]
    public async Task UpdateProfile_RozilikEskirgan_ConsentAcceptedTalabQilinadi_BilanQaytaYoziladi()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100006);
        (await client.PutAsJsonAsync("/api/me/profile", ProfileBody(), TestJson.Options)).StatusCode.Should().Be(HttpStatusCode.OK);

        Guid studentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.PublicUsers.AsNoTracking().SingleAsync(u => u.TelegramId == 760100006);
            var student = await db.Students.SingleAsync(s => s.PublicUserId == user.Id);
            studentId = student.Id;
            student.RecordConsent(DateTimeOffset.UtcNow.AddDays(-30), "0.9", parentalConsent: false, now: DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var withoutConsent = await client.PutAsJsonAsync("/api/me/profile", new { phone = "+998911112233" }, TestJson.Options);
        withoutConsent.StatusCode.Should().Be(HttpStatusCode.BadRequest, "eskirgan rozilik — saqlashda ham qayta rozilik shart (sessiya bilan bir xil)");
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(withoutConsent);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("consentAccepted");

        var withConsent = await client.PutAsJsonAsync("/api/me/profile", new { consentAccepted = true }, TestJson.Options);
        withConsent.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await withConsent.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        dto.ConsentCurrent.Should().BeTrue();
        dto.ConsentVersion.Should().Be("1.0");
        (await CountAssessmentsAsync(studentId)).Should().Be(0);
    }

    [Fact]
    public async Task UpdateProfile_NotogriFormat_400ValidatorXatosi()
    {
        await SeedSpaceAsync();
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100007);

        var response = await client.PutAsJsonAsync("/api/me/profile", new { phone = "12345", email = "not-an-email" }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("phone", "email");
    }

    [Fact]
    public async Task UpdateProfile_PutBilanYaratilganProfil_KeyinSessiyaBoshTanaBilanOchiladi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROFU1", 1);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 760100008);
        (await client.PutAsJsonAsync("/api/me/profile", ProfileBody(), TestJson.Options)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Profil `PUT` bilan yaratilgan — `POST /api/me/sessions` `{}` bilan uni topadi va anketa so'ramaydi.
        var start = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);

        start.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await start.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        result.Resumed.Should().BeFalse();
        (await CountAssessmentsAsync((await LoadStudentAsync(760100008)).Id)).Should().Be(1);
    }

    [Fact]
    public async Task UpdateProfile_AutentifikatsiyaSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/me/profile", ProfileBody(), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

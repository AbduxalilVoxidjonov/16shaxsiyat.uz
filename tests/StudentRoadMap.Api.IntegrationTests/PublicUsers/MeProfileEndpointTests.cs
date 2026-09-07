using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.GetStudentProfile;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// `MeProfileEndpointTests` uchun alohida host — `POST /api/me/sessions` IP bo'yicha 10/soat
/// (`RateLimitSetup.PublicStartSession`), bitta host ichida shu kvota bo'linadi. Ikki sinf →
/// ikki host → ikki kvota.
/// </summary>
public sealed class PublicUserProfileApiTestFactory : TelegramApiTestFactory
{
}

/// <summary>`StartPublicSessionWithProfileEndpointTests` uchun alohida host (kvota sababi yuqorida).</summary>
public sealed class PublicUserProfileEditApiTestFactory : TelegramApiTestFactory
{
}

/// <summary>Ikkala sinf uchun umumiy yordamchilar.</summary>
internal static class ProfileTestSupport
{
    public static object FullBody(
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
            languageCode = "uz",
            programCode = TestDataFactory.DefaultProgramCode,
        };

    public static async Task SeedSpaceAndProgramAsync(TelegramApiTestFactory factory, string testCode, int order)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        await TestDataFactory.CreatePublishedTestAsync(db, now, testCode, order, questionCount: 2);
    }

    public static async Task<HttpClient> AuthenticatedClientAsync(TelegramApiTestFactory factory, long telegramId, string? lastName = null)
    {
        var client = factory.CreateClient();
        var payload = TelegramLoginTestSupport.BuildSignedPayload(telegramId, firstName: "Ali", lastName: lastName);
        var response = await client.PostAsJsonAsync("/api/auth/telegram", payload, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TelegramLoginResult>(TestJson.Options);
        client.UseBearer(body!.AccessToken);
        return client;
    }

    /// <summary>`400 VALIDATION_ERROR` javobidagi `errors` lug'atining kalitlari.</summary>
    public static async Task<(string Code, IReadOnlyList<string> ErrorKeys)> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var code = problem.GetProperty("code").GetString()!;
        var keys = problem.TryGetProperty("errors", out var errors)
            ? errors.EnumerateObject().Select(p => p.Name).ToList()
            : [];
        return (code, keys);
    }
}

/// <summary>
/// `GET /api/me/profile` (`docs/07` §5.1a) va `POST /api/me/sessions` ning PROFIL YO'Q holati:
/// to'liq to'plam majburiy, xato maydon nomlari bilan `VALIDATION_ERROR`.
/// </summary>
public sealed class MeProfileEndpointTests : IClassFixture<PublicUserProfileApiTestFactory>
{
    private readonly PublicUserProfileApiTestFactory _factory;

    public MeProfileEndpointTests(PublicUserProfileApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_ProfilYoq_HasProfileFalseVaTelegramIsmidanFishTaklifi()
    {
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100001, lastName: "Valiyev");

        var response = await client.GetAsync(new Uri("/api/me/profile", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "anketa hali to'ldirilmagani xato emas — `404` qaytarilmaydi");
        var profile = (await response.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        profile.HasProfile.Should().BeFalse();
        profile.FullName.Should().BeNull();
        profile.BirthDate.Should().BeNull();
        profile.Phone.Should().BeNull();
        profile.ConsentCurrent.Should().BeFalse();
        profile.SuggestedFullName.Should().Be("Valiyev Ali", "Familiya Ism tartibida — foydalanuvchi tahrirlaydi");
    }

    [Fact]
    public async Task Profile_ProfilYoq_TelegramdaFaqatIsm_TaklifIsmdanIborat()
    {
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100002);

        var profile = await client.GetFromJsonAsync<MyStudentProfileDto>("/api/me/profile", TestJson.Options);

        profile!.SuggestedFullName.Should().Be("Ali");
    }

    [Fact]
    public async Task Profile_AutentifikatsiyaSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/me/profile", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartSession_ProfilYoq_BoshTana_400BarchaMajburiyMaydonlarBilan()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROF1", 1);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100003);

        var response = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "birinchi sessiyada anketa to'liq bo'lishi shart");
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("fullName", "birthDate", "gender", "phone", "consentAccepted");
    }

    [Fact]
    public async Task StartSession_ProfilYoq_RoziliksIz_400ConsentAccepted()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROF2", 2);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100004);

        var response = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(consentAccepted: false), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("consentAccepted");
    }

    [Fact]
    public async Task StartSession_ProfilYoq_VoyagaYetmagan_OtaOnaRoziligiSiz_400ParentalConsent()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROF3", 3);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100005);
        var minorBirthYear = DateTime.UtcNow.Year - 15;

        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            ProfileTestSupport.FullBody(birthYear: minorBirthYear, grade: 9, parentalConsent: false),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("parentalConsent");
    }

    [Fact]
    public async Task Profile_SessiyadanKeyin_SaqlanganAnketaniQaytaradi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROF4", 4);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100006, lastName: "Valiyev");

        var start = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(), TestJson.Options);
        start.StatusCode.Should().Be(HttpStatusCode.Created);

        var profile = (await client.GetFromJsonAsync<MyStudentProfileDto>("/api/me/profile", TestJson.Options))!;

        profile.HasProfile.Should().BeTrue();
        profile.FullName.Should().Be("Karimov Sardor Alisherovich");
        profile.BirthDate.Should().Be(new DateOnly(1995, 4, 12));
        profile.Gender.Should().Be(Gender.Male);
        profile.Phone.Should().Be("+998901234567");
        profile.Grade.Should().BeNull("`Student.NoGrade` mijozga `null` sifatida chiqadi");
        profile.Email.Should().BeNull();
        profile.ConsentVersion.Should().Be("1.0");
        profile.ConsentCurrent.Should().BeTrue();
        profile.ParentalConsent.Should().BeFalse();
        profile.IsMinor.Should().BeFalse();
        profile.SuggestedFullName.Should().Be("Valiyev Ali", "taklif profil bor holatda ham qaytadi — mijoz e'tiborsiz qoldiradi");
    }

    [Fact]
    public async Task StartSession_ProfilBor_VoyagaYetmagan_BazadaOtaOnaRoziligiBor_QaytaSoralmaydi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROF5", 5);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 740100007);
        var minorBirthYear = DateTime.UtcNow.Year - 15;

        var first = await client.PostAsJsonAsync(
            "/api/me/sessions",
            ProfileTestSupport.FullBody(birthYear: minorBirthYear, grade: 9, parentalConsent: true),
            TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var profile = (await client.GetFromJsonAsync<MyStudentProfileDto>("/api/me/profile", TestJson.Options))!;
        profile.IsMinor.Should().BeTrue();
        profile.ParentalConsent.Should().BeTrue();
        profile.Grade.Should().Be(9);

        // Bo'sh tana: ota-ona roziligi bazada `true` — qayta so'ralmaydi, sessiya davom etadi.
        var second = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.Resumed.Should().BeTrue();
    }
}

/// <summary>
/// `POST /api/me/sessions` — PROFIL BOR holati (`docs/07` §5.4, 2026-09-07): shaxsiy maydonlar
/// qayta so'ralmaydi, kelganlari tahrir sifatida qo'llanadi, rozilik faqat eskirganda.
/// </summary>
public sealed class StartPublicSessionWithProfileEndpointTests : IClassFixture<PublicUserProfileEditApiTestFactory>
{
    private readonly PublicUserProfileEditApiTestFactory _factory;

    public StartPublicSessionWithProfileEndpointTests(PublicUserProfileEditApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_ProfilBor_BoshTanaDavomEttiradi_FaqatProgramCodeYangiSessiyaOchadi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROFE1", 1);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 750100001);

        var first = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(), TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        // 1) Bo'sh tana — tugallanmagan sessiya davom ettiriladi, anketa so'ralmaydi.
        var resumed = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);
        resumed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resumed.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.Resumed.Should().BeTrue();

        // Sessiyani tashlab ketilgan qilamiz — keyingi chaqiruv YANGI sessiya ochishi kerak.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assessment = await db.Assessments.SingleAsync(a => a.Id == firstBody.AssessmentId);
            assessment.MarkAbandoned(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        // 2) Faqat `programCode` — shaxsiy ma'lumot YUBORILMAYDI, profil bazadan olinadi.
        var second = await client.PostAsJsonAsync(
            "/api/me/sessions",
            new { programCode = TestDataFactory.DefaultProgramCode },
            TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.Created, "profil bazada bor — to'liq anketa talab qilinmaydi");
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        secondBody.Resumed.Should().BeFalse();
        secondBody.AssessmentId.Should().NotBe(firstBody.AssessmentId);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newAssessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.Id == secondBody.AssessmentId);
        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == newAssessment.StudentId);
        student.FullName.Should().Be("Karimov Sardor Alisherovich", "bo'sh maydonlar bazadagini o'zgartirmaydi");
        student.BirthDate.Should().Be(new DateOnly(1995, 4, 12));
        student.Phone.Value.Should().Be("+998901234567");
        (await verifyDb.Students.CountAsync(s => s.Id == student.Id)).Should().Be(1, "bitta akkaunt → bitta profil");
    }

    [Fact]
    public async Task StartSession_RozilikEskirgan_ConsentAcceptedSiz400_BilanQaytaYoziladi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROFE2", 2);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 750100002);

        var first = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(), TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessmentId = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.AssessmentId;

        Guid studentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            studentId = (await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == assessmentId)).StudentId;
            var student = await db.Students.SingleAsync(s => s.Id == studentId);
            // Roziliknoma matni yangilangan — foydalanuvchi eski versiyaga rozi bo'lgan.
            student.RecordConsent(DateTimeOffset.UtcNow.AddDays(-30), "0.9", parentalConsent: false, now: DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var profile = (await client.GetFromJsonAsync<MyStudentProfileDto>("/api/me/profile", TestJson.Options))!;
        profile.ConsentCurrent.Should().BeFalse("anketa shu bayroq bo'yicha rozilik blokini ko'rsatadi");

        var withoutConsent = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);
        withoutConsent.StatusCode.Should().Be(HttpStatusCode.BadRequest, "eskirgan rozilik — qayta rozilik shart");
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(withoutConsent);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("consentAccepted");

        var withConsent = await client.PostAsJsonAsync("/api/me/sessions", new { consentAccepted = true }, TestJson.Options);
        withConsent.StatusCode.Should().Be(HttpStatusCode.OK, "boshqa maydon so'ralmaydi — sessiya davom etadi");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == studentId);
        updated.ConsentVersion.Should().Be("1.0", "rozilik joriy versiya bilan qayta yozildi");
    }

    [Fact]
    public async Task StartSession_ProfilBor_KelganMaydonlarTahrirSifatidaQollanadi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROFE3", 3);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 750100003);

        var first = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(grade: 9), TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessmentId = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.AssessmentId;

        DateTimeOffset consentGivenAtBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var studentId = (await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == assessmentId)).StudentId;
            consentGivenAtBefore = (await db.Students.AsNoTracking().SingleAsync(s => s.Id == studentId)).ConsentGivenAt;
        }

        // "O'zgartirish": F.I.Sh., telefon, email yangilanadi; `grade: 0` — "maktabda o'qimayman";
        // tug'ilgan sana va jins YUBORILMAYDI → o'zgarmaydi.
        var edit = await client.PostAsJsonAsync(
            "/api/me/sessions",
            new
            {
                fullName = "Karimova Malika Alisherovna",
                phone = "+998911112233",
                email = "malika@example.com",
                grade = 0,
            },
            TestJson.Options);

        edit.StatusCode.Should().Be(HttpStatusCode.OK, "tugallanmagan sessiya davom etadi, tahrir esa saqlanadi");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.Id == assessmentId);
        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.FullName.Should().Be("Karimova Malika Alisherovna");
        student.NormalizedName.Should().Be("KARIMOVA MALIKA ALISHEROVNA");
        student.Phone.Value.Should().Be("+998911112233");
        student.Email.Should().Be("malika@example.com");
        student.Grade.Should().Be(Student.NoGrade, "`0` — sinfni aniq \"yo'q\" qilish");
        student.BirthDate.Should().Be(new DateOnly(1995, 4, 12), "yuborilmagan maydon o'zgarmaydi");
        student.Gender.Should().Be(Gender.Male);
        student.ConsentVersion.Should().Be("1.0");
        student.ConsentGivenAt.Should().Be(consentGivenAtBefore, "rozilik joriy — isbot sanasi qayta yozilmaydi");
    }

    [Fact]
    public async Task StartSession_ProfilBor_NotogriFormatdagiMaydon_400ValidatorXatosi()
    {
        await ProfileTestSupport.SeedSpaceAndProgramAsync(_factory, "PROFE4", 4);
        using var client = await ProfileTestSupport.AuthenticatedClientAsync(_factory, 750100004);

        var first = await client.PostAsJsonAsync("/api/me/sessions", ProfileTestSupport.FullBody(), TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Format tekshiruvi validatorda QOLGAN — kelgan maydon noto'g'ri bo'lsa rad etiladi.
        var response = await client.PostAsJsonAsync("/api/me/sessions", new { phone = "12345" }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (code, keys) = await ProfileTestSupport.ReadValidationProblemAsync(response);
        code.Should().Be("VALIDATION_ERROR");
        keys.Should().BeEquivalentTo("phone");
    }
}

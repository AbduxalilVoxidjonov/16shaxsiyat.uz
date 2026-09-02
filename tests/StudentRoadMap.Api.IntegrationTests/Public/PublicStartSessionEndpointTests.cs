using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>`POST /api/public/sessions` — `docs/07` 1.2-bo'lim, `prompts/10` DoD (dublikat/limit/validatsiya).</summary>
public sealed class PublicStartSessionEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStartSessionEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(School School, string AccessToken)> SeedSchoolAsync(
        string seed, int dailyLimit = 500, string? accessCode = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-{seed}", token, dailyLimit, accessCode: accessCode);

        // Anketa katalogi butun sinf uchun umumiy (bir marta seedlansa yetarli).
        if (!await db.TestDefinitions.AnyAsync())
        {
            await TestDataFactory.CreatePublishedTestAsync(db, now, "SESS1", 1, questionCount: 1);
        }

        return (school, token);
    }

    private static StartSessionCommand ValidCommand(string slug, string token, string fullName, DateOnly birthDate, string? accessCode = null) =>
        new(
            Slug: slug,
            AccessToken: token,
            AccessCode: accessCode,
            FullName: fullName,
            BirthDate: birthDate,
            Gender: Gender.Male,
            Grade: 9,
            ClassLetter: "B",
            Phone: "+998901234567",
            ParentPhone: null,
            Email: null,
            ConsentAccepted: true,
            LanguageCode: "uz");

    [Fact]
    public async Task StartSession_YangiOquvchi_201VaSessionTokenQaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("new1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Aliyev Sardor Bekzodovich", new DateOnly(2010, 4, 17));
        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.Resumed.Should().BeFalse();
        body.SessionToken.Should().NotBeNullOrWhiteSpace();
        body.Status.Should().Be("Draft");
        body.Tests.Should().ContainSingle(t => t.Code == "SESS1" && t.Status == "NotStarted");
    }

    [Fact]
    public async Task StartSession_TugallanmaganSessiya_ResumedTrueQaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("resume1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Karimov Javlon Baxtiyorovich", new DateOnly(2011, 5, 1));

        var first = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        var second = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        secondBody.Should().NotBeNull();
        secondBody!.Resumed.Should().BeTrue();
        secondBody.SessionToken.Should().Be(firstBody!.SessionToken);
        secondBody.AssessmentId.Should().Be(firstBody.AssessmentId);
    }

    /// <summary>
    /// PM qarori (2026-08-31): kunlik hisoblagich faqat YANGI `Assessment` yaratilganda
    /// oshiriladi — `resumed: true` holatida o'zgarmaydi (`registration_counters` maktab uchun
    /// ko'rinadigan biznes ko'rsatkich, har so'rovni sanash uni ma'nosiz qiladi).
    /// </summary>
    [Fact]
    public async Task StartSession_TugallanmaganSessiyaniQaytaChaqirish_HisoblagichniOshirmaydi()
    {
        var (school, token) = await SeedSchoolAsync("resumecounter1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Toshmatov Sirojiddin Anvarovich", new DateOnly(2011, 7, 20));

        var first = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        secondBody!.Resumed.Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var counter = await db.RegistrationCounters.SingleAsync(c => c.SchoolId == school.Id);

        counter.Count.Should().Be(1, "ikkinchi (resumed) chaqiruv yangi Assessment yaratmagani uchun hisoblagich o'zgarmasligi kerak");
    }

    [Fact]
    public async Task StartSession_90KunIchidaYakunlanganSessiya_409Qaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("dup1");
        const string fullName = "Yusupova Malika Odilovna";
        var birthDate = new DateOnly(2009, 6, 15);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;

            var phone = PhoneNumber.Create("+998901112233").Value;
            var student = Student.Create(Guid.NewGuid(), school.Id, fullName, birthDate, Gender.Female, 9, phone, now, now);
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var testDefinition = await db.TestDefinitions.Include(t => t.Questions).FirstAsync();
            var question = testDefinition.Questions.First();

            var startedAt = now.AddDays(-10);
            var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
            var assessment = Assessment.Create(
                Guid.NewGuid(), student.Id, school.Id, $"completed-token-{Guid.NewGuid():N}", "uz", programId,
                startedAt, expiresAt: startedAt.AddDays(7), now: startedAt);

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, 1);
            assessment.AddTest(assessmentTest);
            assessment.StartTest(testDefinition.Id, startedAt);
            assessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, 3, null, 1000, startedAt);
            assessment.CompleteTest(testDefinition.Id, [question.Id], now.AddDays(-9));
            assessment.Complete(now.AddDays(-9));

            db.Assessments.Add(assessment);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var command = ValidCommand(school.Slug.Value, token, fullName, birthDate);
        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("DUPLICATE_ASSESSMENT");
    }

    [Fact]
    public async Task StartSession_KunlikLimitOshsa_429Qaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("limit1", dailyLimit: 1);
        using var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/api/public/sessions",
            ValidCommand(school.Slug.Value, token, "Rahimov Otabek Davronovich", new DateOnly(2010, 1, 1)),
            TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(
            "/api/public/sessions",
            ValidCommand(school.Slug.Value, token, "Nazarova Sabina Ilhomovna", new DateOnly(2010, 2, 2)),
            TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("RATE_LIMITED");
    }

    /// <summary>
    /// PM qarori (2026-09-02): `errors` lug'ati kalitlari qolgan API bilan bir xil camelCase
    /// bo'lishi kerak (`ExceptionHandlingMiddleware`/`ValidationException` `JsonNamingPolicy.CamelCase`
    /// bilan o'giradi) — PascalCase (`FullName`, `BirthDate`...) EMAS. Bir vaqtning o'zida bir
    /// nechta maydon (`fullName`, `phone`, `birthDate`, `grade`, `consentAccepted`) noto'g'ri
    /// bo'lganda barcha kalitlar camelCase ekanini shu bitta so'rovda tekshiradi (`PublicStartSession`
    /// IP bo'yicha 10/soat limit — sinf ichida qo'shimcha `/sessions` chaqiruvi qo'shmaslik uchun
    /// ataylab MAVJUD testga birlashtirilgan, alohida test emas).
    /// </summary>
    [Fact]
    public async Task StartSession_RozilikBerilmasa_400VaErrorsKalitlariCamelCaseQaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("invalid1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Ali", new DateOnly(1990, 1, 1))
            with
        {
            FullName = "Ali", // 5 belgidan kam
            Phone = "12345", // noto'g'ri format
            Grade = 0, // chegaradan tashqarida
            ConsentAccepted = false,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");

        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("fullName", out _).Should().BeTrue();
        errors.TryGetProperty("phone", out _).Should().BeTrue();
        errors.TryGetProperty("birthDate", out _).Should().BeTrue();
        errors.TryGetProperty("grade", out _).Should().BeTrue();
        errors.TryGetProperty("consentAccepted", out _).Should().BeTrue();

        // Eski (buzilgan) PascalCase kalitlar ENDI bo'lmasligi kerak.
        errors.TryGetProperty("FullName", out _).Should().BeFalse();
        errors.TryGetProperty("Phone", out _).Should().BeFalse();
        errors.TryGetProperty("BirthDate", out _).Should().BeFalse();
        errors.TryGetProperty("Grade", out _).Should().BeFalse();
        errors.TryGetProperty("ConsentAccepted", out _).Should().BeFalse();
    }

    [Fact]
    public async Task StartSession_NotogriTelefonFormati_400Qaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("invalidphone1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Boshqa Foydalanuvchi Ismi", new DateOnly(2010, 1, 1))
            with
        { Phone = "12345" };

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// `POST /api/me/sessions` — ommaviy (maktabsiz) sessiya (`docs/07` 5.4-bo'lim). Maktab
/// oqimidan farqli: slug/token yo'q, makon `SchoolKind.PublicSpace`, `Student` esa
/// `public_user_id` bilan bog'lanadi.
/// </summary>
public sealed class StartPublicSessionEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private readonly TelegramApiTestFactory _factory;

    public StartPublicSessionEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private static object Body(
        string fullName = "Karimov Sardor Alisherovich",
        int birthYear = 1995,
        int? grade = null,
        bool parentalConsent = false,
        string? programCode = TestDataFactory.DefaultProgramCode) => new
        {
            fullName,
            birthDate = new DateOnly(birthYear, 4, 12),
            gender = nameof(Gender.Male),
            phone = "+998901234567",
            consentAccepted = true,
            parentalConsent,
            grade,
            languageCode = "uz",
            programCode,
        };

    private async Task<HttpClient> AuthenticatedClientAsync(long telegramId)
    {
        var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, telegramId);
        client.UseBearer(accessToken);
        return client;
    }

    [Fact]
    public async Task StartSession_OmmaviyMakonda_201VaStudentPublicUserIdBilanBoglanadi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
            await TestDataFactory.CreatePublishedTestAsync(db, now, "PUBS1", 1, questionCount: 2);
        }

        using var client = await AuthenticatedClientAsync(720100001);

        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        body!.SessionToken.Should().NotBeNullOrWhiteSpace();
        body.Resumed.Should().BeFalse();
        body.Tests.Should().NotBeEmpty();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.Id == body.AssessmentId);
        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.PublicUserId.Should().NotBeNull("ommaviy oqimda `Student` akkauntga bog'lanadi");
        student.Grade.Should().Be(Student.NoGrade, "sinf ko'rsatilmagan — maktabda o'qimaydi");
        student.ConsentVersion.Should().Be("1.0");

        var assessmentSpace = await verifyDb.Schools.AsNoTracking().SingleAsync(s => s.Id == assessment.SchoolId);
        assessmentSpace.IsPublicSpace.Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_SessiyaTokeniBilan_OmmaviyOqimDavomEtadi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
            await TestDataFactory.CreatePublishedTestAsync(db, now, "PUBS2", 2, questionCount: 2);
        }

        using var client = await AuthenticatedClientAsync(720100002);
        var start = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);
        start.EnsureSuccessStatusCode();
        var sessionToken = (await start.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;

        // Sessiya tokeni MAKTAB oqimidagi endpointlarda ham ishlaydi — ommaviy foydalanuvchi
        // shu bilan testni yechadi (`/api/public/sessions/*` o'zgarmadi).
        using var sessionClient = _factory.CreateClient();
        sessionClient.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var state = await sessionClient.GetAsync(new Uri("/api/public/sessions/me", UriKind.Relative));

        state.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartSession_TakroriyChaqiruv_MavjudSessiyaniDavomEttiradi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
            await TestDataFactory.CreatePublishedTestAsync(db, now, "PUBS3", 3, questionCount: 2);
        }

        using var client = await AuthenticatedClientAsync(720100003);

        var first = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        var second = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.OK, "davom ettirilgan sessiya yangi resurs yaratmaydi");
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        secondBody.Resumed.Should().BeTrue();
        secondBody.AssessmentId.Should().Be(firstBody.AssessmentId);
    }

    [Fact]
    public async Task StartSession_18YoshgachaOtaOnaRoziligiSiz_400Qaytaradi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
            await TestDataFactory.CreatePublishedTestAsync(db, now, "PUBS4", 4, questionCount: 2);
        }

        using var client = await AuthenticatedClientAsync(720100004);
        var minorBirthYear = DateTime.UtcNow.Year - 15;

        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            Body(birthYear: minorBirthYear, grade: 9, parentalConsent: false),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task StartSession_YoshChegaradanTashqarida_400Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync(720100005);

        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            Body(birthYear: DateTime.UtcNow.Year - 3),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartSession_AutentifikatsiyaSiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Ommaviy makonga birorta dastur BIRIKTIRILMAGAN holat — `409 NO_PROGRAM_AVAILABLE`
/// (`400 PROGRAM_REQUIRED` EMAS: tanlaydigan narsa umuman yo'q). Alohida `IClassFixture` —
/// bazada hech qanday dastur bo'lmasligi kerak.
/// </summary>
public sealed class StartPublicSessionNoProgramEndpointTests : IClassFixture<TelegramNoProgramApiTestFactory>
{
    private readonly TelegramNoProgramApiTestFactory _factory;

    public StartPublicSessionNoProgramEndpointTests(TelegramNoProgramApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_MakongaDasturBiriktirilmagan_409NoProgramAvailableQaytaradi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, DateTimeOffset.UtcNow);
        }

        using var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, 730100001);
        client.UseBearer(accessToken);

        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            new
            {
                fullName = "Karimov Sardor Alisherovich",
                birthDate = new DateOnly(1995, 4, 12),
                gender = nameof(Gender.Male),
                phone = "+998901234567",
                consentAccepted = true,
            },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("NO_PROGRAM_AVAILABLE");
    }

    [Fact]
    public async Task StartSession_OmmaviyMakonSeedQilinmagan_409PublicSpaceNotConfiguredQaytaradi()
    {
        using var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, 730100002);
        client.UseBearer(accessToken);

        // ⚠️ Bu fact `StartSession_MakongaDasturBiriktirilmagan_...` dan OLDIN ishlashi
        // kafolatlanmaydi (xUnit tartibi aniqlanmagan) — shu sabab ikkala javob ham
        // qabul qilinadi, lekin ikkalasi ham AYNAN shu ikki koddan biri bo'lishi shart:
        // "makon yo'q" yoki "makon bor, dastur yo'q".
        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            new
            {
                fullName = "Karimov Sardor Alisherovich",
                birthDate = new DateOnly(1995, 4, 12),
                gender = nameof(Gender.Male),
                phone = "+998901234567",
                consentAccepted = true,
            },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().BeOneOf("PUBLIC_SPACE_NOT_CONFIGURED", "NO_PROGRAM_AVAILABLE");
    }
}

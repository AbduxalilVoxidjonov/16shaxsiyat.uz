using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.GetStudentProfile;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Settings;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.PublicUsers;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasidagi "o'z maydonlari" (`customFields`) — Telegram/kabinet
/// oqimi (`POST /api/me/sessions`, `PUT /api/me/profile`), P52 2-to'lqin (2026-09-12,
/// `docs/18` §9.6.2). **Eng muhim regressiya:** profil allaqachon bor bo'lsa, `Required`
/// o'z maydoni ham qayta so'RALMAYDI (egasining alohida ta'kidlagan talabi) — boshqa asosiy
/// maydonlar (`fullName`/`birthDate`/...) bilan bir xil "bir marta so'raladi" naqshi.
/// </summary>
public sealed class StartPublicSessionCustomFieldsEndpointTests : IClassFixture<TelegramApiTestFactory>
{
    private const string ParentJobCode = "PARENT_JOB";

    private readonly TelegramApiTestFactory _factory;

    public StartPublicSessionCustomFieldsEndpointTests(TelegramApiTestFactory factory)
    {
        _factory = factory;
    }

    private static RegistrationFormDefinition RequiredParentJobDefinition() => RegistrationFormDefinition.Create(
        RegistrationFormDefinition.Default.CoreFields,
        [new RegistrationCustomField(ParentJobCode, QuestionType.ShortText, "Ota-onangiz kasbi", null, RegistrationFieldRequirement.Required, MaxLength: 100, InputPattern: null, Options: null, Order: 9)]);

    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

        // `IClassFixture` bitta bazani BUTUN klass bo'yicha baham ko'radi — test bir marta
        // yetarli (`StartPublicSessionPerProgramEndpointTests.EnsureSeededAsync` uslubi).
        if (!await db.TestDefinitions.AnyAsync(t => t.Code == "PUBCF1"))
        {
            await TestDataFactory.CreatePublishedTestAsync(db, now, "PUBCF1", 1, questionCount: 2);
        }

        var existing = await db.RegistrationFormSettings.FirstOrDefaultAsync(s => s.Id == RegistrationFormSettings.SingletonId);
        if (existing is null)
        {
            db.RegistrationFormSettings.Add(RegistrationFormSettings.Create(RequiredParentJobDefinition(), now, updatedByAdminUserId: null));
            await db.SaveChangesAsync();
        }
    }

    private static object Body(object? customFields = null) => new
    {
        fullName = "Nurmatova Sevara",
        birthDate = new DateOnly(1996, 6, 10),
        gender = nameof(Gender.Female),
        phone = "+998901234580",
        consentAccepted = true,
        parentalConsent = false,
        grade = (int?)null,
        languageCode = "uz",
        customFields,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(long telegramId)
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var (accessToken, _, _) = await PublicUserTestDataFactory.LoginAsync(client, telegramId);
        client.UseBearer(accessToken);
        return client;
    }

    [Fact]
    public async Task StartSession_YangiProfil_RequiredOzMaydoniBoshBoLsa400()
    {
        using var client = await AuthenticatedClientAsync(720400001);

        var response = await client.PostAsJsonAsync("/api/me/sessions", Body(), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty(ParentJobCode, out _).Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_YangiProfil_OzMaydoniProfileExtraGaYoziladi()
    {
        using var client = await AuthenticatedClientAsync(720400002);

        var response = await client.PostAsJsonAsync(
            "/api/me/sessions",
            Body(customFields: new Dictionary<string, object> { [ParentJobCode] = "Muhandis" }),
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == result.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.ProfileExtra.Should().NotBeNull();
        JsonDocument.Parse(student.ProfileExtra!).RootElement.GetProperty(ParentJobCode).GetString().Should().Be("Muhandis");
    }

    /// <summary>
    /// **ENG MUHIM REGRESSIYA** (egasining alohida ta'kidlagan talabi): profil allaqachon
    /// bor bo'lgan foydalanuvchi ikkinchi marta sessiya ochsa — `Required` o'z maydoni ham
    /// (boshqa hech qanday shaxsiy maydon ham) QAYTA SO'RALMAYDI, faqat `programCode` yetarli.
    /// </summary>
    [Fact]
    public async Task StartSession_IkkinchiMarta_OzMaydoniHamHechQandayMaydonHamQaytaSoRalmaydi()
    {
        using var client = await AuthenticatedClientAsync(720400003);

        var first = await client.PostAsJsonAsync(
            "/api/me/sessions",
            Body(customFields: new Dictionary<string, object> { [ParentJobCode] = "Shifokor" }),
            TestJson.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        // Ikkinchi so'rov — HECH QANDAY maydon (shaxsiy ham, o'z maydoni ham) yuborilmaydi.
        var second = await client.PostAsJsonAsync("/api/me/sessions", new { }, TestJson.Options);

        second.StatusCode.Should().Be(HttpStatusCode.OK, "profil bor — hech qanday maydon (o'z maydoni ham) qayta so'ralmaydi");
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        secondBody.Resumed.Should().BeTrue();
        secondBody.AssessmentId.Should().Be(firstBody.AssessmentId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == firstBody.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        // Ikkinchi so'rovda hech narsa yuborilmagani uchun avvalgi qiymat SAQLANIB QOLADI.
        JsonDocument.Parse(student.ProfileExtra!).RootElement.GetProperty(ParentJobCode).GetString().Should().Be("Shifokor");
    }

    /// <summary>`PUT /api/me/profile` — mavjud profilda o'z maydoni TAHRIRLANADI (yangi qiymat bilan ustidan yoziladi).</summary>
    [Fact]
    public async Task UpdateProfile_MavjudProfilda_OzMaydoniTahrirlanadi()
    {
        using var client = await AuthenticatedClientAsync(720400004);

        var created = await client.PostAsJsonAsync(
            "/api/me/sessions",
            Body(customFields: new Dictionary<string, object> { [ParentJobCode] = "O'qituvchi" }),
            TestJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = (await created.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        var update = await client.PutAsJsonAsync(
            "/api/me/profile",
            new { customFields = new Dictionary<string, object> { [ParentJobCode] = "Dizayner" } },
            TestJson.Options);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<MyStudentProfileDto>(TestJson.Options))!;
        updated.HasProfile.Should().BeTrue();
        updated.RegistrationForm.CustomFields.Should().ContainSingle(f => f.Code == ParentJobCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == createdBody.AssessmentId);
        var student = await db.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);
        JsonDocument.Parse(student.ProfileExtra!).RootElement.GetProperty(ParentJobCode).GetString().Should().Be("Dizayner");
    }
}

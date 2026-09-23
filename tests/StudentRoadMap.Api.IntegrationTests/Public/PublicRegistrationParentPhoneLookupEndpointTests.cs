using System.Net;
using System.Net.Http.Json;
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
/// 2026-09-23 egasi qarori (`docs/18` §9.6): standartda o'quvchining o'z telefoni IXTIYORIY,
/// ota-ona telefoni MAJBURIY. `birthDate` ham kiritilmagan holatda o'quvchini topish (BR-1/
/// BR-5) F.I.Sh. + ota-ona telefoni bo'yicha ishlashini qulflaydi. Alohida `IClassFixture` —
/// `PublicStartSession` rate limiter kvotasi (10/soat/IP) `PublicRegistrationFieldsEndpointTests`
/// bilan bo'linmasligi uchun (u klass kvotani to'liq ishlatadi).
/// </summary>
public sealed class PublicRegistrationParentPhoneLookupEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicRegistrationParentPhoneLookupEndpointTests(PublicApiTestFactory factory)
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
    public async Task StartSession_OptionalBirthDate_TelefonYoQ_OtaOnaTelefoniBilanOquvchiTopiladiVaSessiyaTiklanadi()
    {
        // 2026-09-23 egasi qarori: o'z telefoni ixtiyoriy, ota-ona telefoni majburiy. `birthDate`
        // ham kiritilmagan bo'lsa, qidiruv kaliti F.I.Sh. + OTA-ONA TELEFONI bo'ladi — aks holda
        // qaytib kelgan o'quvchining yarim qolgan sessiyasi (BR-5) yo'qolardi.
        var (school, accessToken, programCode) = await SeedProgramAsync("ob3");

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
            fullName = "Qodirova Madina",
            gender = "Female",
            grade = 7,
            parentPhone = "+998901234599",
            consentAccepted = true,
            languageCode = "uz",
            programCode,
        };

        var first = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);
        var second = await client.PostAsJsonAsync("/api/public/sessions", Body(), TestJson.Options);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = (await first.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
        var secondBody = (await second.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;

        secondBody.Resumed.Should().BeTrue("ota-ona telefoni bo'yicha yarim qolgan sessiya tiklanishi kerak");
        secondBody.AssessmentId.Should().Be(firstBody.AssessmentId);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.Students.AsNoTracking().CountAsync(s => s.SchoolId == school.Id)).Should().Be(1);
        var student = await db2.Students.AsNoTracking().SingleAsync(s => s.SchoolId == school.Id);
        student.Phone.Should().BeNull();
        student.ParentPhone!.Value.Should().Be("+998901234599");
    }
}

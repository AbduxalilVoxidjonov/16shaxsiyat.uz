using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Settings;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// Dastur darajasidagi ustunlik (`docs/18` §9.6.2, P52 2-to'lqin, 2026-09-12, egasining qarori):
/// GLOBAL ro'yxatdan o'tish formasi sozlamasida `birthDate`/`grade` `Optional`/`Hidden` bo'lsa
/// ham, ILMIY BATAREYASI bor dasturda ball normalari yosh/sinfga tayangani sabab bular
/// `Required`ga ko'tariladi (hisoblanadi, saqlanmaydi — `RegistrationFormResolver`).
///
/// Alohida `IClassFixture` — `PublicRegistrationFieldsEndpointTests` bilan bitta klassda
/// `PublicStartSession` rate limiter kvotasi (10/soat/IP) bo'linib ketmasligi uchun.
/// </summary>
public sealed class PublicRegistrationFormBatteryOverrideTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicRegistrationFormBatteryOverrideTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>`gender` bu ustunlikka KIRMAYDI (`Optional` sozlama shu holatda ham hurmat qilinadi).</summary>
    [Fact]
    public async Task StartSession_BatareyaliDastur_GlobalOptionalBoLsaHamBirthDateVaGradeMajburiy()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var core = RegistrationFormDefinition.Default.CoreFields with
        {
            BirthDate = RegistrationFormDefinition.Default.CoreFields.BirthDate with { Requirement = RegistrationFieldRequirement.Optional },
            Grade = RegistrationFormDefinition.Default.CoreFields.Grade with { Requirement = RegistrationFieldRequirement.Optional },
            Gender = RegistrationFormDefinition.Default.CoreFields.Gender with { Requirement = RegistrationFieldRequirement.Optional },
        };
        db.RegistrationFormSettings.Add(RegistrationFormSettings.Create(
            RegistrationFormDefinition.Create(core, []), now, updatedByAdminUserId: null));

        var accessToken = TestDataFactory.NewAccessToken("battover1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battover1", accessToken);
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "BATTOVER1", 1);

        var program = AssessmentProgram.Create(
            Guid.NewGuid(), "BATTOVER-PROG-1", "Batareyali dastur (sinov)", now,
            visibility: ProgramVisibility.Assigned);
        program.AddTest(mbtiTest.Id, 1, isPersonalityBatteryTest: true, now);
        program.Publish(now, hasPersonalityBattery: true);

        db.AssessmentPrograms.Add(program);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        // `birthDate`/`grade` ATAYLAB yuborilmaydi — GLOBAL sozlamada `Optional`, lekin
        // batareya ustunligi ularni `Required`ga ko'taradi.
        var body = new
        {
            slug = school.Slug.Value,
            accessToken,
            fullName = "Sobirova Kamola",
            phone = "+998901234573",
            parentPhone = "+998909998877",
            consentAccepted = true,
            languageCode = "uz",
            programCode = program.Code,
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "batareyali dasturda `birthDate`/`grade` GLOBAL sozlamadan qat'i nazar majburiy");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("birthDate", out _).Should().BeTrue();
        errors.TryGetProperty("grade", out _).Should().BeTrue();
        errors.TryGetProperty("gender", out _).Should().BeFalse("`gender` batareya ustunligiga kirmaydi — GLOBAL `Optional` sozlamasi hurmat qilinadi");
    }
}

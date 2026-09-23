using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// BR-1/BR-5 **`(o'quvchi, dastur)` juftligi** bo'yicha (`docs/02` BR-1, `docs/07` §1.2,
/// egasining qarori 2026-09-07): maktabga ikki dastur (A, B) biriktirilgan, o'quvchi ikkalasini
/// ham topshira oladi. Har test O'Z maktabi va O'Z (`Assigned`) dasturlarini yaratadi — `Public`
/// ko'rinishdagi dastur barcha maktabda ko'rinib qolgani uchun fixture ichidagi testlar
/// bir-biriga ta'sir qilmasin.
/// </summary>
public sealed class PublicStartSessionPerProgramEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStartSessionPerProgramEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(School School, string AccessToken, string ProgramCodeA, string ProgramCodeB, string TestCodeA, string TestCodeB);

    private async Task<Seeded> SeedSchoolWithTwoProgramsAsync(string seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken($"pp{seed}");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-pp{seed}", accessToken);

        var testA = await TestDataFactory.CreateStandaloneTestAsync(db, now, $"PPA{seed}", 1, questionCount: 1);
        var testB = await TestDataFactory.CreateStandaloneTestAsync(db, now, $"PPB{seed}", 2, questionCount: 1);
        var programA = await TestDataFactory.CreateProgramAsync(db, now, $"PP-A-{seed}", [(testA.Id, 1)], ProgramVisibility.Assigned);
        var programB = await TestDataFactory.CreateProgramAsync(db, now, $"PP-B-{seed}", [(testB.Id, 1)], ProgramVisibility.Assigned);

        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, programA.Id, now));
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, programB.Id, now));
        await db.SaveChangesAsync();

        return new Seeded(school, accessToken, programA.Code, programB.Code, testA.Code, testB.Code);
    }

    private static StartSessionCommand Command(Seeded seeded, string fullName, string? programCode) =>
        new(
            Slug: seeded.School.Slug.Value,
            AccessToken: seeded.AccessToken,
            AccessCode: null,
            FullName: fullName,
            BirthDate: new DateOnly(2010, 3, 3),
            Gender: Gender.Male,
            Grade: 9,
            ClassLetter: "A",
            Phone: "+998901234567",
            ParentPhone: "+998909998877",
            Email: null,
            ConsentAccepted: true,
            LanguageCode: "uz",
            ProgramCode: programCode);

    private async Task<StartSessionResult> StartAsync(HttpClient client, StartSessionCommand command, HttpStatusCode expected)
    {
        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.StatusCode.Should().Be(expected);
        return (await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!;
    }

    private async Task CompleteAsync(Guid assessmentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await AssessmentCompletionHelper.CompleteAsync(db, assessmentId, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StartSession_ADasturYakunlangan_AQayta_409DuplicateAssessment()
    {
        var seeded = await SeedSchoolWithTwoProgramsAsync("1");
        const string fullName = "Perprogram Birinchi Talaba";
        using var client = _factory.CreateClient();

        var first = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.Created);
        await CompleteAsync(first.AssessmentId);

        var response = await client.PostAsJsonAsync("/api/public/sessions", Command(seeded, fullName, seeded.ProgramCodeA), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "bir xil dastur 90 kun ichida qayta topshirilmaydi (BR-1)");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("DUPLICATE_ASSESSMENT");
    }

    [Fact]
    public async Task StartSession_ADasturYakunlangan_BDastur_201YangiSessiya()
    {
        var seeded = await SeedSchoolWithTwoProgramsAsync("2");
        const string fullName = "Perprogram Ikkinchi Talaba";
        using var client = _factory.CreateClient();

        var first = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.Created);
        await CompleteAsync(first.AssessmentId);

        var second = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeB), HttpStatusCode.Created);

        second.Resumed.Should().BeFalse("boshqa dastur — takror emas, yangi sessiya");
        second.AssessmentId.Should().NotBe(first.AssessmentId);
        second.Tests.Should().ContainSingle(t => t.Code == seeded.TestCodeB, "B sessiyasida faqat B dasturining testlari bo'ladi");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstRow = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == first.AssessmentId);
        var secondRow = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == second.AssessmentId);
        secondRow.StudentId.Should().Be(firstRow.StudentId, "ikkala sessiya BIR o'quvchiga tegishli — yangi o'quvchi yaratilmaydi");
        secondRow.ProgramId.Should().NotBe(firstRow.ProgramId);
    }

    [Fact]
    public async Task StartSession_ADaYarimQolgan_AQayta_200Resumed()
    {
        var seeded = await SeedSchoolWithTwoProgramsAsync("3");
        const string fullName = "Perprogram Uchinchi Talaba";
        using var client = _factory.CreateClient();

        var first = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.Created);

        var second = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.OK);

        second.Resumed.Should().BeTrue();
        second.AssessmentId.Should().Be(first.AssessmentId);
    }

    [Fact]
    public async Task StartSession_ADaYarimQolgan_BDastur_201YangiVaIkkalasiTugallanmagan()
    {
        var seeded = await SeedSchoolWithTwoProgramsAsync("4");
        const string fullName = "Perprogram Tortinchi Talaba";
        using var client = _factory.CreateClient();

        var first = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.Created);

        var second = await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeB), HttpStatusCode.Created);

        second.Resumed.Should().BeFalse("A dagi yarim qolgan sessiya B ni 'davom ettirish' deb qaytarilmaydi");
        second.AssessmentId.Should().NotBe(first.AssessmentId);
        second.SessionToken.Should().NotBe(first.SessionToken);
        second.Tests.Should().ContainSingle(t => t.Code == seeded.TestCodeB);

        // Yangi semantika: bir o'quvchida bir vaqtda IKKI tugallanmagan sessiya (har dasturda bittadan).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstRow = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == first.AssessmentId);
        var unfinished = await db.Assessments.AsNoTracking()
            .Where(a => a.StudentId == firstRow.StudentId && (a.Status == AssessmentStatus.Draft || a.Status == AssessmentStatus.InProgress))
            .ToListAsync();

        unfinished.Should().HaveCount(2);
        unfinished.Select(a => a.ProgramId).Distinct().Should().HaveCount(2, "har dasturda ko'pi bilan bitta tugallanmagan sessiya");
        firstRow.Status.Should().Be(AssessmentStatus.Draft, "A sessiyasi `Abandoned` qilinmaydi — u hali muddati o'tmagan");
    }

    /// <summary>
    /// Tartib: dastur tanlash sessiya holatidan OLDIN. Ikki dastur + `programCode` yo'q →
    /// `400 PROGRAM_REQUIRED`, hatto o'quvchida yarim qolgan sessiya bo'lsa ham (qaysi dasturni
    /// davom ettirish noma'lum).
    /// </summary>
    [Fact]
    public async Task StartSession_YarimQolganBorLekinProgramCodeYoq_400ProgramRequired()
    {
        var seeded = await SeedSchoolWithTwoProgramsAsync("5");
        const string fullName = "Perprogram Beshinchi Talaba";
        using var client = _factory.CreateClient();

        await StartAsync(client, Command(seeded, fullName, seeded.ProgramCodeA), HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("/api/public/sessions", Command(seeded, fullName, programCode: null), TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_REQUIRED");
    }
}

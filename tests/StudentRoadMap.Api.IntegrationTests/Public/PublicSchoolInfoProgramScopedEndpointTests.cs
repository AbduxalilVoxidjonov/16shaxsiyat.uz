using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/schools/{slug}` — `tests[]` endi DASTURGA bog'liq (`docs/07` 1.1-bo'lim,
/// P52, 2026-09-11 jonli hodisadan keyin). Egasining aynan sinovdan o'tkazgan stsenariysi:
/// maktabga bitta dastur biriktirilgan (`FORMS`), katalogda BOSHQA dasturning (arxivlangan
/// `PERSONALITY_PROFILE`) nashr qilingan testlari ham bor — kirish ekrani ularni KO'RSATMASLIGI
/// kerak (ilgari `GetSchoolInfoQueryHandler` butun katalogdan olardi, dastur filtri YO'Q edi).
///
/// Bu klass shuningdek kirish ekrani (`GetSchoolInfo`) va sessiya boshlanishi (`StartSession`)
/// BIR XIL test ro'yxatini berishini qulflaydi (`ProgramTestCatalog` — umumiy manba) — aks
/// holda o'quvchi ekranda ko'rgan test soni bilan sessiyada olgan test soni farq qilishi mumkin.
/// </summary>
public sealed class PublicSchoolInfoProgramScopedEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSchoolInfoProgramScopedEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSchoolInfo_DasturArxivlangan_TestsFaqatFaolDasturnikiBoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken("archived-program-scope");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-archived-program-scope", accessToken);

        // Arxivlanadigan dastur — katalogda `Published &amp;&amp; IsActive` test bor, LEKIN dastur
        // o'zi arxivlangan (egasining jonli holati: "Shaxsiyat profili" arxivlandi).
        var archivedTest = await TestDataFactory.CreateStandaloneTestAsync(db, now, "ARCHIVEDPT1", 1, questionCount: 2);
        var archivedProgram = await TestDataFactory.CreateProgramAsync(
            db, now, "ARCHIVED-PP-1", [(archivedTest.Id, 1)], ProgramVisibility.Public);

        var archivedProgramTracked = await db.AssessmentPrograms.SingleAsync(p => p.Id == archivedProgram.Id);
        archivedProgramTracked.Archive(now);
        await db.SaveChangesAsync();

        // Maktabga biriktirilgan YAGONA faol dastur.
        var activeTest = await TestDataFactory.CreateStandaloneTestAsync(db, now, "ACTIVEPT1", 1, questionCount: 2);
        var activeProgram = await TestDataFactory.CreateProgramAsync(
            db, now, "ACTIVE-PP-1", [(activeTest.Id, 1)], ProgramVisibility.Assigned);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, activeProgram.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSchoolInfoResult>(TestJson.Options);
        body.Should().NotBeNull();

        body!.Programs.Should().ContainSingle();
        body.Programs[0].Code.Should().Be(activeProgram.Code);

        body.Tests.Should().ContainSingle(t => t.Code == activeTest.Code);
        body.Tests.Should().NotContain(t => t.Code == archivedTest.Code, "arxivlangan dasturning testi kirish ekranida ko'rinmasligi kerak");
    }

    [Fact]
    public async Task GetSchoolInfoVaStartSession_BirXilTestRoyxatiniBeradiVaFaolSavolisizTestniTushiradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken("lockstep-scope");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-lockstep-scope", accessToken);

        var testA = await TestDataFactory.CreateStandaloneTestAsync(db, now, "LOCKA1", 1, questionCount: 2);
        var testB = await TestDataFactory.CreateStandaloneTestAsync(db, now, "LOCKB1", 2, questionCount: 1);
        var testC = await TestDataFactory.CreateStandaloneTestAsync(db, now, "LOCKC1", 3, questionCount: 2);

        // `testB`ning yagona savoli faolsizlantiriladi — ikkala oqim ham (kirish ekrani va
        // sessiya) bu testni ro'yxatdan tushirib qoldirishi shart.
        var onlyQuestion = await db.Questions.SingleAsync(q => q.TestDefinitionId == testB.Id);
        onlyQuestion.Deactivate();
        await db.SaveChangesAsync();

        var program = await TestDataFactory.CreateProgramAsync(
            db, now, "LOCKSTEP-PP-1", [(testA.Id, 1), (testB.Id, 2), (testC.Id, 3)], ProgramVisibility.Assigned);
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        var infoResponse = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative));
        infoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var info = await infoResponse.Content.ReadFromJsonAsync<GetSchoolInfoResult>(TestJson.Options);
        info.Should().NotBeNull();

        var expectedCodes = new[] { testA.Code, testC.Code };

        info!.Tests.Select(t => t.Code).Should().Equal(expectedCodes, "testB faol savoliga ega emas — kirish ekranidan tushib qolishi shart");
        info.Programs.Should().ContainSingle();
        info.Programs[0].Tests.Select(t => t.Code).Should().Equal(expectedCodes);

        var startCommand = new StartSessionCommand(
            Slug: school.Slug.Value,
            AccessToken: accessToken,
            AccessCode: null,
            FullName: "Lockstep Talaba",
            BirthDate: new DateOnly(2010, 5, 5),
            Gender: Gender.Female,
            Grade: 9,
            ClassLetter: "A",
            Phone: "+998901234568",
            ParentPhone: "+998909998877",
            Email: null,
            ConsentAccepted: true,
            LanguageCode: "uz",
            ProgramCode: program.Code);

        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", startCommand, TestJson.Options);
        startResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var started = await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        started.Should().NotBeNull();

        started!.Tests.Select(t => t.Code).Should().Equal(
            expectedCodes, "kirish ekrani va sessiya BIR XIL manbadan (`ProgramTestCatalog`) foydalanishi kerak");
    }

    [Fact]
    public async Task GetSchoolInfo_IkkiDasturBittaTestUmumiy_YuqoriDarajadagiTestsBirlashtiriladiVaTakrorlanmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var accessToken = TestDataFactory.NewAccessToken("union-scope");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-union-scope", accessToken);

        var sharedTest = await TestDataFactory.CreateStandaloneTestAsync(db, now, "UNIONSHARED1", 1, questionCount: 2);
        var onlyA = await TestDataFactory.CreateStandaloneTestAsync(db, now, "UNIONA1", 2, questionCount: 2);
        var onlyB = await TestDataFactory.CreateStandaloneTestAsync(db, now, "UNIONB1", 2, questionCount: 2);

        var programA = await TestDataFactory.CreateProgramAsync(
            db, now, "UNION-PP-A", [(sharedTest.Id, 1), (onlyA.Id, 2)], ProgramVisibility.Assigned);
        var programB = await TestDataFactory.CreateProgramAsync(
            db, now, "UNION-PP-B", [(sharedTest.Id, 1), (onlyB.Id, 2)], ProgramVisibility.Assigned);

        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, programA.Id, now));
        db.SchoolPrograms.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, programB.Id, now));
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={accessToken}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSchoolInfoResult>(TestJson.Options);
        body.Should().NotBeNull();

        body!.Programs.Should().HaveCount(2);
        body.Programs.Single(p => p.Code == programA.Code).Tests.Select(t => t.Code)
            .Should().Equal(sharedTest.Code, onlyA.Code);
        body.Programs.Single(p => p.Code == programB.Code).Tests.Select(t => t.Code)
            .Should().Equal(sharedTest.Code, onlyB.Code);

        body.Tests.Should().HaveCount(3, "birlashma — umumiy test IKKI marta hisoblanmaydi");
        body.Tests.Select(t => t.Code).Should().BeEquivalentTo([sharedTest.Code, onlyA.Code, onlyB.Code]);
        body.Tests.Count(t => t.Code == sharedTest.Code).Should().Be(1);

        body.TotalEstimatedMinutes.Should().Be(body.Tests.Sum(t => t.EstimatedMinutes));
    }
}

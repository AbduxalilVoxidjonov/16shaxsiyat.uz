using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence.Seeding;

/// <summary>
/// `DbSeeder.SeedSurveysAsync` — namunaviy (mijozga xos) tarmoqlanuvchi so'rovnoma seedi
/// (`docs/18` §7). Egasining talabi: "bir marta qo'shiladi, hech qachon ustidan yozilmaydi" —
/// bu TIZIM METODIKALARIDAGI (`DbSeederTests`, `DbSeederAtomicityAndBr8Tests`) "qayta
/// sinxronlash" mantig'idan TUBDAN farqli, shu sabab alohida fayl/sinov.
/// </summary>
public sealed class DbSeederSurveyTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "srm-dbseeder-survey-tests-" + Guid.NewGuid());

    public DbSeederSurveyTests()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "surveys"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private DbSeeder NewSeeder(AppDbContext context) =>
        new(context, new FixedDateTimeProvider(Now), EmptyConfiguration(), new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance, _tempRoot);

    /// <summary>Minimal, lekin `docs/18` §2.2–§2.4 elementlarini (bo'lim, shart, variant) qamraydigan sinov so'rovnomasi.</summary>
    private void WriteMiniSurvey(string q1TextUz)
    {
        var json = $$"""
            {
              "code": "MINI-SURVEY", "nameUz": "Mini so'rovnoma", "descriptionUz": "Sinov",
              "displayOrder": 1, "estimatedMinutes": 1, "pageSize": 10, "shuffleQuestions": false,
              "scoringMode": "Survey",
              "sections": [
                { "code": "S1", "titleUz": "Bo'lim 1", "descriptionUz": null, "displayOrder": 1, "visibility": null }
              ],
              "questions": [
                { "code": "MS-Q1", "order": 1, "sectionCode": "S1", "textUz": "{{q1TextUz}}", "type": "SingleChoice", "scale": "SURVEY", "direction": 1, "weight": 1.0, "isRequired": true,
                  "options": [ { "textUz": "Ha", "value": 1, "displayOrder": 1 }, { "textUz": "Yo'q", "value": 2, "displayOrder": 2 } ] },
                { "code": "MS-Q2", "order": 2, "sectionCode": "S1", "textUz": "Izoh", "type": "ShortText", "scale": "SURVEY", "direction": 1, "weight": 1.0, "isRequired": false,
                  "placeholder": "Yozing", "maxLength": 100,
                  "visibility": { "match": "All", "conditions": [ { "questionCode": "MS-Q1", "operator": "Equals", "values": [1] } ] } }
              ]
            }
            """;

        File.WriteAllText(Path.Combine(_tempRoot, "surveys", "mini-survey.json"), json);
    }

    [Fact]
    public async Task SeedAsync_YangiSorovnoma_CustomDraftHolatidaBolimVaShartBilanQoshiladi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        WriteMiniSurvey("Savol matni");

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context).SeedAsync();
        }

        await using var verify = NewContext(connection);
        var test = await verify.TestDefinitions
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .Include(t => t.Sections)
            .SingleAsync(t => t.Code == "MINI-SURVEY");

        test.Kind.Should().Be(TestKind.Custom);
        test.IsSystem.Should().BeFalse();
        test.ScoringMode.Should().Be(TestScoringMode.Survey);
        test.Status.Should().Be(TestDefinitionStatus.Draft, "namunaviy so'rovnoma HECH QACHON avtomatik nashr qilinmaydi (docs/18 §7)");
        test.Sections.Should().ContainSingle(s => s.Code == "S1");
        test.QuestionCount.Should().Be(2);

        var q2 = test.Questions.Single(q => q.Code == "MS-Q2");
        q2.VisibilityRule.Should().NotBeNull("MS-Q2 shartli savol");
        q2.SectionId.Should().Be(test.Sections.Single().Id);

        var q1 = test.Questions.Single(q => q.Code == "MS-Q1");
        q1.Options.Should().HaveCount(2);
    }

    /// <summary>
    /// Eng muhim test (topshiriq talabi): qo'lda tahrirlangan (superadmin savol matnini
    /// o'zgartirgan) anketa qayta seeddan keyin ham O'ZGARMAGAN qolishi kerak — tizim
    /// metodikalaridagi "matn/tartib qayta sinxronlanadi" mantig'i bu yerda ISHLAMAYDI.
    /// </summary>
    [Fact]
    public async Task SeedAsync_QoldaTahrirlanganSorovnomaniQaytaSeedOzgartirmaydi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        WriteMiniSurvey("Asl savol matni");

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context).SeedAsync();
        }

        // Superadmin panelda savol matnini tahrirlagan — deb faraz qilinadi.
        await using (var context = NewContext(connection))
        {
            var editedQuestion = await context.Questions.SingleAsync(q => q.Code == "MS-Q1");
            editedQuestion.UpdateText("Superadmin tahrirlagan matn", null, null);
            await context.SaveChangesAsync();
        }

        // Seed faylida matn "o'zgargan" (masalan, yangi versiya fayldan kelgan) — lekin bu
        // e'tiborsiz qoldirilishi kerak, chunki `code` ('MINI-SURVEY') bazada allaqachon bor.
        WriteMiniSurvey("Faylda YANGI matn (e'tiborsiz qoldirilishi kerak)");

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context).SeedAsync();
        }

        await using var verify = NewContext(connection);
        var test = await verify.TestDefinitions.SingleAsync(t => t.Code == "MINI-SURVEY");

        var question = await verify.Questions.SingleAsync(q => q.Code == "MS-Q1");
        question.TextUz.Should().Be("Superadmin tahrirlagan matn", "qayta seed superadmin tahririni bosib o'tmasligi kerak");

        (await verify.Questions.CountAsync(q => q.TestDefinitionId == test.Id)).Should().Be(2, "qayta seedda dublikat savol qo'shilmasligi kerak");
    }

    [Fact]
    public async Task SeedAsync_SurveysKatalogiYoqBolsa_XatoBermayOtkazibYuboradi()
    {
        Directory.Delete(Path.Combine(_tempRoot, "surveys"));

        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        Func<Task> act = async () =>
        {
            await using var context = NewContext(connection);
            await NewSeeder(context).SeedAsync();
        };

        await act.Should().NotThrowAsync("'surveys' katalogi topilmasa seed jimgina o'tkazib yuborishi kerak (test bankidagi bilan bir xil naqsh)");
    }
}

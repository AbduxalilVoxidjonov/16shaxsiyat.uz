using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence.Seeding;

/// <summary>
/// `DbSeeder` — BR-8 (scale/direction/weight qulfi) va S8 (bitta tranzaksiya) sinovlari.
/// Haqiqiy 4 seed faylini o'zgartirmaslik shart bo'lgani uchun (topshiriq sharti) bu yerda
/// **vaqtinchalik seed katalogi** (`DbSeeder`ning testga mo'ljallangan `seedDataRoot`
/// parametri orqali) ishlatiladi — mazmuni to'liq nazorat qilinadi.
/// </summary>
public sealed class DbSeederAtomicityAndBr8Tests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "srm-dbseeder-tests-" + Guid.NewGuid());

    public DbSeederAtomicityAndBr8Tests()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "test-definitions"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static AppDbContext NewContext(Microsoft.Data.Sqlite.SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private DbSeeder NewSeeder(AppDbContext context, IConfiguration? configuration = null) =>
        new(context, new FixedDateTimeProvider(Now), configuration ?? EmptyConfiguration(), new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance, _tempRoot);

    private void WriteMiniTestDefinition(string q1TextUz, string q1Scale, string q2TextUz, string q2Scale)
    {
        var json = $$"""
            {
              "code": "MINI", "nameUz": "Mini test", "descriptionUz": "Sinov", "displayOrder": 1,
              "estimatedMinutes": 1, "pageSize": 10, "shuffleQuestions": false,
              "questions": [
                { "code": "MINI-Q1", "order": 1, "textUz": "{{q1TextUz}}", "type": "Likert5", "scale": "{{q1Scale}}", "direction": 1, "weight": 1.0, "isRequired": true },
                { "code": "MINI-Q2", "order": 2, "textUz": "{{q2TextUz}}", "type": "Likert5", "scale": "{{q2Scale}}", "direction": 1, "weight": 1.0, "isRequired": true }
              ]
            }
            """;

        File.WriteAllText(Path.Combine(_tempRoot, "test-definitions", "mini.json"), json);
    }

    private void WriteValidTypeCatalog()
    {
        const string json = """
            [
              { "code": "AAAA", "nameUz": "Sinov tipi", "shortDescriptionUz": "Qisqa", "longDescriptionUz": "Uzun tavsif", "strengths": ["a"], "growthAreas": ["b"], "careerHints": ["c"] }
            ]
            """;

        File.WriteAllText(Path.Combine(_tempRoot, "type-catalog.json"), json);
    }

    private void WriteCorruptTypeCatalog()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "type-catalog.json"), "{ bu yaroqsiz JSON [[[");
    }

    [Fact]
    public async Task SeedAsync_WhenScaleChangedBetweenRuns_ThrowsAndLeavesDatabaseUnchanged()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        WriteMiniTestDefinition("Savol 1", "A", "Savol 2", "B");

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context).SeedAsync();
        }

        // Seed faylida MINI-Q2 shkalasi "B" dan "ZZZZ" ga "o'zgartirilgan" — BR-8 buzilishi.
        WriteMiniTestDefinition("Savol 1", "A", "Savol 2", "ZZZZ");

        Func<Task> act = async () =>
        {
            await using var context = NewContext(connection);
            await NewSeeder(context).SeedAsync();
        };

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().ContainAny("scale", "MINI-Q2");

        await using var verify = NewContext(connection);
        var question = await verify.Questions.SingleAsync(q => q.Code == "MINI-Q2");
        question.Scale.Should().Be("B", "BR-8 konflikti aniqlanganda hech qanday o'zgarish saqlanmasligi kerak");
    }

    [Fact]
    public async Task SeedAsync_WhenLaterPhaseFails_RollsBackAlreadySucceededPhaseInSameCall()
    {
        // S8: to'rt bosqich (test-ta'riflari → tip katalogi → kasb xaritasi → superadmin) bitta
        // tranzaksiyada. Bu sinov test-ta'riflari bosqichi MUVAFFAQIYATLI saqlanganidan KEYIN
        // tip katalogi bosqichi ishlamay qolgan holatni simulyatsiya qiladi — agar tranzaksiya
        // ishlamasa, test-ta'riflaridagi matn yangilanishi (rollback qilinmagan holda) saqlanib
        // qolgan bo'lardi.
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        WriteMiniTestDefinition("Eski matn", "A", "Savol 2", "B");
        WriteValidTypeCatalog();

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context).SeedAsync();
        }

        // Test-ta'riflari o'zgarishi (matn — konfliktsiz, muvaffaqiyatli saqlanadi) + tip
        // katalogi buzilgan JSON (keyingi bosqich xato beradi).
        WriteMiniTestDefinition("YANGI MATN (rollback qilinishi kerak)", "A", "Savol 2", "B");
        WriteCorruptTypeCatalog();

        Func<Task> act = async () =>
        {
            await using var context = NewContext(connection);
            await NewSeeder(context).SeedAsync();
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verify = NewContext(connection);
        var question = await verify.Questions.SingleAsync(q => q.Code == "MINI-Q1");
        question.TextUz.Should().Be("Eski matn", "keyingi bosqich (tip katalogi) xato bergani uchun butun tranzaksiya (shu jumladan test-ta'riflari yangilanishi) rollback qilinishi kerak");
    }
}

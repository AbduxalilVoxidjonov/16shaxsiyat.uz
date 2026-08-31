using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence.Seeding;

/// <summary>
/// `DbSeeder` — Docker/PostgreSQL bu muhitda yo'q (`prompts/04`), shu sabab **SQLite in-memory**
/// ustida ishga tushiriladi (haqiqiy tranzaksiya/rollback beruvchi relational provayder,
/// InMemory'dan farqli — `SqliteAppDbContextFactory` izohiga qarang). Haqiqiy 4 seed JSON'i,
/// `type-catalog.json` va `career-map.json` — `Infrastructure/Persistence/SeedData/`dan csproj
/// orqali nusxalangan (fayllar tahrirlanmagan).
/// </summary>
public sealed class DbSeederTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const int ExpectedTestDefinitionCount = 4;
    private const int ExpectedQuestionCount = 190; // 60 + 50 + 48 + 32 (docs/03, 9-bo'lim)
    private const int ExpectedTypeCatalogCount = 16;
    private const int ExpectedCareerMapCount = 18;

    private static IConfiguration BuildConfiguration(string username = "superadmin", string password = "Sup3rSecret!Pass") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_USERNAME"] = username,
                ["ADMIN_PASSWORD"] = password,
            })
            .Build();

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static DbSeeder NewSeeder(AppDbContext context, IConfiguration configuration, string? seedDataRoot = null) =>
        new(context, new FixedDateTimeProvider(Now), configuration, new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance, seedDataRoot);

    [Fact]
    public async Task SeedAsync_CalledTwiceWithRealFixtures_ProducesStableRowCountsAndCreatesSuperadmin()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var configuration = BuildConfiguration();

        // --- 1-marta ---
        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using (var verify1 = NewContext(connection))
        {
            (await verify1.TestDefinitions.CountAsync()).Should().Be(ExpectedTestDefinitionCount);
            (await verify1.Questions.CountAsync()).Should().Be(ExpectedQuestionCount);
            (await verify1.TypeCatalog.CountAsync()).Should().Be(ExpectedTypeCatalogCount);
            (await verify1.CareerMap.CountAsync()).Should().Be(ExpectedCareerMapCount);
            (await verify1.AdminUsers.CountAsync()).Should().Be(1);
        }

        // --- 2-marta (idempotentlik) ---
        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using var verify2 = NewContext(connection);
        (await verify2.TestDefinitions.CountAsync()).Should().Be(ExpectedTestDefinitionCount, "ikkinchi seed dublikat test-ta'rifi yaratmasligi kerak");
        (await verify2.Questions.CountAsync()).Should().Be(ExpectedQuestionCount, "ikkinchi seed dublikat savol yaratmasligi kerak");
        (await verify2.TypeCatalog.CountAsync()).Should().Be(ExpectedTypeCatalogCount, "type_catalog qayta seedda takrorlanmasligi kerak");
        (await verify2.CareerMap.CountAsync()).Should().Be(ExpectedCareerMapCount, "career_map qayta seedda takrorlanmasligi kerak");
        (await verify2.AdminUsers.CountAsync()).Should().Be(1, "superadmin ikkinchi marta qayta yaratilmasligi kerak");

        var admin = await verify2.AdminUsers.SingleAsync();
        admin.Username.Should().Be("superadmin");
        new Pbkdf2PasswordHasher().Verify("Sup3rSecret!Pass", admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WithoutAdminCredentials_DoesNotCreateSuperadmin()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var emptyConfiguration = new ConfigurationBuilder().Build();

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, emptyConfiguration).SeedAsync();
        }

        await using var verify = NewContext(connection);
        (await verify.AdminUsers.CountAsync()).Should().Be(0);
        (await verify.TestDefinitions.CountAsync()).Should().Be(ExpectedTestDefinitionCount, "boshqa bosqichlar admin bo'lmasa ham davom etishi kerak");
    }
}

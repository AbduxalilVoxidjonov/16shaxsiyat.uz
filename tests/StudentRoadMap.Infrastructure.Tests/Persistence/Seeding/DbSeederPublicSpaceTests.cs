using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence.Seeding;

/// <summary>
/// Ommaviy makon seed'i — P47. `DbSeederTests` bilan bir xil muhit (SQLite in-memory).
/// Haqiqiy PostgreSQL ustidagi tekshiruv `Migrations.Tests/SeedIdempotencyTests` da.
/// </summary>
public sealed class DbSeederPublicSpaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_USERNAME"] = "superadmin",
                ["ADMIN_PASSWORD"] = "Sup3rSecret!Pass",
            })
            .Build();

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static DbSeeder NewSeeder(AppDbContext context, IConfiguration configuration) =>
        new(context, new FixedDateTimeProvider(Now), configuration, new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance);

    [Fact]
    public async Task SeedAsync_OmmaviyMakonniYaratadi_VaTakroriySeedDaDublikatYoq()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var configuration = BuildConfiguration();

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        string firstAccessToken;
        await using (var verify = NewContext(connection))
        {
            var spaces = await verify.Schools.IgnoreQueryFilters()
                .Where(s => s.Kind == SchoolKind.PublicSpace)
                .ToListAsync();

            spaces.Should().HaveCount(1);

            var space = spaces[0];
            space.Id.Should().Be(DbSeeder.PublicSpaceSchoolId, "identifikator deterministik bo'lishi shart");
            space.Slug.Value.Should().Be(DbSeeder.PublicSpaceSlug);
            space.Name.Should().Be(DbSeeder.PublicSpaceName);
            space.ShowResultToStudent.Should().BeTrue();
            space.IsActive.Should().BeTrue();
            space.AccessToken.Should().NotBeNullOrWhiteSpace();

            firstAccessToken = space.AccessToken;
        }

        // --- 2-marta (idempotentlik) ---
        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using var verify2 = NewContext(connection);
        var afterSecond = await verify2.Schools.IgnoreQueryFilters()
            .Where(s => s.Kind == SchoolKind.PublicSpace)
            .ToListAsync();

        afterSecond.Should().HaveCount(1, "takroriy seed ikkinchi ommaviy makon yaratmasligi kerak");
        afterSecond[0].AccessToken.Should().Be(firstAccessToken, "token qayta generatsiya qilinsa mavjud ommaviy havolalar o'lardi");
    }

    [Fact]
    public async Task SeedAsync_OddiyMaktablargaTegmaydi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();

            context.Schools.Add(School.Create(
                Guid.NewGuid(),
                "12-son maktab",
                "Farg'ona",
                "Qo'qon",
                SchoolSlug.Create("12-son-maktab").Value,
                "maktab-tokeni",
                Now));

            await context.SaveChangesAsync();
        }

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, BuildConfiguration()).SeedAsync();
        }

        await using var verify = NewContext(connection);
        (await verify.Schools.CountAsync()).Should().Be(2, "1 maktab + 1 ommaviy makon");

        var school = await verify.Schools.SingleAsync(s => s.Kind == SchoolKind.School);
        school.ShowResultToStudent.Should().BeFalse("mavjud maktablar uchun standart qiymat eski global sozlamadagidek");
        school.Kind.Should().Be(SchoolKind.School);
    }
}

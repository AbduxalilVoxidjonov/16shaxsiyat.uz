using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `DbSeeder.SeedPromptTemplatesAsync` — `prompts/16` DoD: "Prompt shabloni seed qilinadi va
/// versiyasi `AiAnalysis`ga yoziladi". Alohida test klassi (`DbSeederTests`dan mustaqil) —
/// yangi test klasslari kvota/aralashmasligi uchun ajratilgan.
/// </summary>
public sealed class PromptTemplateSeedTests
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
        new(context, new FixedDateTimeProvider(Now), configuration, new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance, seedDataRoot: null);

    [Fact]
    public async Task SeedAsync_CreatesActiveFullAnalysisV1Template()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, BuildConfiguration()).SeedAsync();
        }

        await using var verify = NewContext(connection);
        var templates = await verify.PromptTemplates.ToListAsync();

        templates.Should().ContainSingle();
        var template = templates.Single();
        template.Key.Should().Be(DefaultPromptTemplates.Key);
        template.Version.Should().Be(DefaultPromptTemplates.Version);
        template.IsActive.Should().BeTrue();
        template.SystemText.Should().Be(DefaultPromptTemplates.SystemTextV1);
        template.UserText.Should().Be(DefaultPromptTemplates.UserTextV1);
        template.JsonSchema.Should().Be(AnalysisJsonSchema.RawJson);
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_DoesNotDuplicateTemplate()
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

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using var verify = NewContext(connection);
        (await verify.PromptTemplates.CountAsync()).Should().Be(1, "ikkinchi seed dublikat shablon yaratmasligi kerak");
    }
}

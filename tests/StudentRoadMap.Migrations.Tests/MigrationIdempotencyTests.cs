using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// IDEMPOTENTLIK: `--migrate` bosqichi qayta ishga tushsa (deploy qayta urinishi, konteyner
/// restart'i) xato bermasligi va sxemani o'zgartirmasligi shart.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class MigrationIdempotencyTests(MigrationsPostgresFixture fixture)
{
    [DockerFact]
    public async Task Migrate_IkkiMartaChaqirilsa_XatoBermaydi_VaSxemaOzgarmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("idempotent", ct);

        await using (var first = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await first.Database.MigrateAsync(ct);
        }

        var columnsAfterFirst = await SchemaSnapshot.ColumnsAsync(connectionString, ct);
        var constraintsAfterFirst = await SchemaSnapshot.ConstraintsAsync(connectionString, ct);
        var programsAfterFirst = await SqlHelpers.CountAsync(connectionString, "assessment_programs", ct);

        // Ikkinchi chaqiruv — yangi kontekst bilan (production'da alohida jarayon).
        await using (var second = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            var act = async () => await second.Database.MigrateAsync(ct);
            await act.Should().NotThrowAsync("takroriy `--migrate` xavfsiz bo'lishi shart");

            (await second.Database.GetPendingMigrationsAsync(ct)).Should().BeEmpty();
        }

        (await SchemaSnapshot.ColumnsAsync(connectionString, ct)).Should().BeEquivalentTo(columnsAfterFirst);
        (await SchemaSnapshot.ConstraintsAsync(connectionString, ct)).Should().BeEquivalentTo(constraintsAfterFirst);
        (await SqlHelpers.CountAsync(connectionString, "assessment_programs", ct))
            .Should().Be(programsAfterFirst, "migratsiya ichidagi xom SQL (`WHERE NOT EXISTS`) dublikat yaratmasligi shart");
    }

    /// <summary>
    /// Ma'lumot bilan to'ldirilgan bazada takroriy migratsiya — backfill qadamlari
    /// (`UPDATE ... WHERE program_id IS NULL`) qayta ishlaganda ham zarar keltirmasligi kerak.
    /// </summary>
    [DockerFact]
    public async Task MalumotliBazada_TakroriyMigrate_BacfillNatijasiniBuzmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("idempotentdata", ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
            await SeedRunner.Create(context).SeedAsync(ct);
        }

        var before = await SnapshotCountsAsync(connectionString, ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        (await SnapshotCountsAsync(connectionString, ct)).Should().BeEquivalentTo(before);
    }

    private static async Task<Dictionary<string, long>> SnapshotCountsAsync(string connectionString, CancellationToken ct)
    {
        var tables = new[] { "assessment_programs", "program_tests", "test_definitions", "questions", "admin_users", "prompt_templates" };
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var table in tables)
        {
            counts[table] = await SqlHelpers.CountAsync(connectionString, table, ct);
        }

        return counts;
    }
}

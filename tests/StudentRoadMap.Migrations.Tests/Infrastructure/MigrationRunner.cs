using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Migratsiyalarni ORALIQ NUQTAGACHA qo'llash uchun umumiy yordamchi — "bo'sh bo'lmagan
/// production bazasi" ssenariylarining asosi (`docs/13` §8.1): eski sxemagacha migratsiya →
/// o'sha sxemaga mos ma'lumot → qolgan migratsiyalar.
/// </summary>
internal static class MigrationRunner
{
    /// <summary>Migratsiyalarni ANIQ bir nuqtagacha qo'llaydi (`dotnet ef database update &lt;id&gt;` ekvivalenti).</summary>
    public static async Task MigrateToAsync(string connectionString, string targetMigration, CancellationToken ct)
    {
        await using var context = MigrationsPostgresFixture.CreateContext(connectionString);

        context.Database.GetMigrations().Should().Contain(targetMigration,
            "oraliq nuqta sifatida ishlatilayotgan migratsiya mavjud bo'lishi shart — nomi o'zgargan bo'lsa bu test yangilanishi kerak");

        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(targetMigration, ct);

        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        applied.Should().Contain(targetMigration);
        (await context.Database.GetPendingMigrationsAsync(ct)).Should().NotBeEmpty(
            "oraliq nuqtadan keyin hali qo'llanmagan migratsiyalar qolishi kerak");
    }

    /// <summary>Qolgan barcha migratsiyalarni production'dagi `--migrate` kabi BITTA chaqiruvda qo'llaydi.</summary>
    public static async Task MigrateAllAsync(string connectionString, CancellationToken ct)
    {
        await using var context = MigrationsPostgresFixture.CreateContext(connectionString);
        await context.Database.MigrateAsync(ct);
    }
}

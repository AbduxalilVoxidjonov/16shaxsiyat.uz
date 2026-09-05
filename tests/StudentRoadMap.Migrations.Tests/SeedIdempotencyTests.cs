using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// SEED IDEMPOTENTLIGI HAQIQIY PostgreSQL'da (`docs/05` 4-bo'lim: seed migratsiyaga
/// qo'yilmaydi, `DbSeeder` idempotent). `Infrastructure.Tests` bu mantiqni SQLite ustida
/// tekshiradi — bu yerda esa production provayderi (Npgsql) va MIGRATSIYA bilan qurilgan
/// sxema ustida, ya'ni `docker-compose.yml` dagi haqiqiy `migrate → seed` ketma-ketligida.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class SeedIdempotencyTests(MigrationsPostgresFixture fixture)
{
    /// <summary>Seed ta'sir qiladigan barcha jadvallar — dublikat shu yerda ushlanadi.</summary>
    private static readonly string[] CountedTables =
    [
        "schools",
        "test_definitions",
        "questions",
        "answer_options",
        "test_scales",
        "assessment_programs",
        "program_tests",
        "type_catalog",
        "career_map",
        "prompt_templates",
        "admin_users",
    ];

    /// <summary>
    /// Seeddan keyin ALBATTA bo'sh bo'lmasligi kerak bo'lgan jadvallar — aks holda test
    /// hech narsani tekshirmagan bo'lardi. `answer_options`/`test_scales` bu ro'yxatda YO'Q:
    /// tizim metodikalari faqat `Likert5` savollardan iborat (seed JSON'larida variant
    /// ta'riflari yo'q), shkalalar esa `Custom` anketa konstruktoriga tegishli.
    /// </summary>
    private static readonly string[] MustBePopulatedTables =
    [
        // P47: seed YAGONA ommaviy makonni (`SchoolKind.PublicSpace`) yaratadi.
        "schools",
        "test_definitions",
        "questions",
        "assessment_programs",
        "program_tests",
        "type_catalog",
        "career_map",
        "prompt_templates",
        "admin_users",
    ];

    [DockerFact]
    public async Task Seed_IkkiMartaIshgaTushsa_DublikatYaratmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("seedtwice", ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await SeedRunner.Create(context).SeedAsync(ct);
        }

        var afterFirst = await CountAllAsync(connectionString, ct);

        // Ikkinchi yurish — yangi kontekst (production'da alohida `seed` konteyneri).
        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            var act = async () => await SeedRunner.Create(context).SeedAsync(ct);
            await act.Should().NotThrowAsync("takroriy seed xavfsiz bo'lishi shart");
        }

        var afterSecond = await CountAllAsync(connectionString, ct);

        afterSecond.Should().BeEquivalentTo(afterFirst, "seed idempotent — hech qaysi jadvalda dublikat paydo bo'lmasligi kerak");
    }

    [DockerFact]
    public async Task Seed_TizimMetodikalari_TizimDasturi_VaSuperadmin_BittadanQoladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("seedunique", ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        for (var run = 0; run < 2; run++)
        {
            await using var context = MigrationsPostgresFixture.CreateContext(connectionString);
            await SeedRunner.Create(context).SeedAsync(ct);
        }

        // 4 tizim metodikasi — kod bo'yicha yagona (`ux_test_definitions_code`).
        (await SqlHelpers.CountAsync(connectionString, "test_definitions WHERE is_system", ct))
            .Should().Be(4);
        (await SqlHelpers.CountAsync(
            connectionString,
            "test_definitions WHERE code IN ('MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY')",
            ct)).Should().Be(4);

        // Tizim dasturi — bitta, 4 ta bog'langan test bilan.
        (await SqlHelpers.CountAsync(connectionString, "assessment_programs WHERE is_system", ct))
            .Should().Be(1);
        (await SqlHelpers.CountAsync(connectionString, "program_tests", ct))
            .Should().Be(4);

        // Superadmin — bitta (parol qayta xeshlanmaydi/dublikat qilinmaydi).
        (await SqlHelpers.CountAsync(connectionString, $"admin_users WHERE username = '{SeedRunner.AdminUsername}'", ct))
            .Should().Be(1);
        (await SqlHelpers.CountAsync(connectionString, "admin_users", ct))
            .Should().Be(1);
    }

    private static async Task<Dictionary<string, long>> CountAllAsync(string connectionString, CancellationToken ct)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var table in CountedTables)
        {
            counts[table] = await SqlHelpers.CountAsync(connectionString, table, ct);
        }

        foreach (var table in MustBePopulatedTables)
        {
            counts[table].Should().BeGreaterThan(0,
                "seed `{0}` jadvaliga ma'lumot yozishi kerak — aks holda test hech narsani tekshirmaydi", table);
        }

        return counts;
    }
}

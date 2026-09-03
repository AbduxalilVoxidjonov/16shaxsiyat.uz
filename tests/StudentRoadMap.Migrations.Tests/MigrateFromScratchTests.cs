using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// NOL HOLATDAN: bo'm-bo'sh bazaga barcha migratsiyalar ketma-ket qo'llanadi va natijaviy
/// sxema modeldan qurilgan sxema bilan solishtiriladi.
///
/// Bu — `docs/12` §5 "Migratsiya" qamrovining haqiqiy bajarilishi: ilgari integratsiya
/// testlari `EnsureCreated()` ishlatgani uchun migratsiyalar UMUMAN bajarilmasdi.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class MigrateFromScratchTests(MigrationsPostgresFixture fixture)
{
    [DockerFact]
    public async Task BoshBazada_BarchaMigratsiyalar_XatosizQollanadi()
    {
        var connectionString = await fixture.CreateEmptyDatabaseAsync("scratch", CancellationToken.None);
        await using var context = MigrationsPostgresFixture.CreateContext(connectionString);

        var expected = context.Database.GetMigrations().ToList();
        expected.Should().NotBeEmpty("migratsiyalar sborkasi bo'sh bo'lmasligi kerak");

        // Production'dagi `--migrate` bilan AYNAN bir xil chaqiruv (`Api/Program.cs`):
        // bitta chaqiruvda BUTUN zanjir bajariladi (`docs/13` §8).
        await context.Database.MigrateAsync(CancellationToken.None);

        var applied = (await context.Database.GetAppliedMigrationsAsync(CancellationToken.None)).ToList();
        applied.Should().BeEquivalentTo(expected, "barcha migratsiyalar qo'llanishi shart");

        var pending = await context.Database.GetPendingMigrationsAsync(CancellationToken.None);
        pending.Should().BeEmpty();
    }

    [DockerFact]
    public async Task Migratsiyalar_ModelBilanSinxron_KutilayotganModelOzgarishiYoq()
    {
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pending", CancellationToken.None);
        await using var context = MigrationsPostgresFixture.CreateContext(connectionString);

        // `dotnet ef migrations has-pending-model-changes` ning kod ekvivalenti — model
        // oxirgi migratsiya snapshot'idan uzoqlashib ketmaganini tekshiradi.
        context.Database.HasPendingModelChanges().Should().BeFalse(
            "model migratsiya snapshot'idan uzoqlashgan — `dotnet ef migrations add <Nom>` kerak");
    }

    /// <summary>
    /// ENG KUCHLI TEKSHIRUV: migratsiyalar bilan qurilgan HAQIQIY sxema modeldan
    /// (`EnsureCreated()`) qurilgan sxema bilan bir xilmi. `has-pending-model-changes`
    /// faqat kod darajasidagi (model ↔ snapshot) farqni ko'radi; bu esa xom SQL bilan
    /// yozilgan migratsiya qadamlari natijasini ham tekshiradi.
    /// </summary>
    [DockerFact]
    public async Task MigratsiyaSxemasi_ModeldanQurilganSxemaBilanBirXil()
    {
        var ct = CancellationToken.None;

        var migratedConnectionString = await fixture.CreateEmptyDatabaseAsync("migrated", ct);
        await using (var migrated = MigrationsPostgresFixture.CreateContext(migratedConnectionString))
        {
            await migrated.Database.MigrateAsync(ct);
        }

        var modelConnectionString = await fixture.CreateEmptyDatabaseAsync("model", ct);
        await using (var model = MigrationsPostgresFixture.CreateContext(modelConnectionString))
        {
            await model.Database.EnsureCreatedAsync(ct);
        }

        (await SchemaSnapshot.ExtensionsAsync(migratedConnectionString, ct))
            .Should().BeEquivalentTo(await SchemaSnapshot.ExtensionsAsync(modelConnectionString, ct),
                "Postgres kengaytmalari (pgcrypto, pg_trgm) ikkala yo'lda ham bir xil bo'lishi kerak");

        // ⚠️ `SchemaDriftBaseline` — bugun mavjud, hujjatlashtirilgan farqlar (tuzatish yangi
        // migratsiya talab qiladi). YANGI farq bu yerda darhol ushlanadi.
        var migratedColumns = SchemaDriftBaseline.NormalizeColumns(
            await SchemaSnapshot.ColumnsAsync(migratedConnectionString, ct));

        migratedColumns
            .Should().BeEquivalentTo(await SchemaSnapshot.ColumnsAsync(modelConnectionString, ct),
                "migratsiyalar natijasidagi ustunlar modeldan qurilgan sxema bilan mos kelishi shart");

        (await SchemaSnapshot.ConstraintsAsync(migratedConnectionString, ct))
            .Should().BeEquivalentTo(await SchemaSnapshot.ConstraintsAsync(modelConnectionString, ct),
                "PK/FK/UNIQUE/CHECK cheklovlari mos kelishi shart");

        SchemaDriftBaseline.NormalizeIndexes(await SchemaSnapshot.IndexesAsync(migratedConnectionString, ct))
            .Should().BeEquivalentTo(await SchemaSnapshot.IndexesAsync(modelConnectionString, ct),
                "indekslar (filtrli indekslar ham) mos kelishi shart");
    }
}

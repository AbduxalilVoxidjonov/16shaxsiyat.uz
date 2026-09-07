using System.Text.RegularExpressions;
using FluentAssertions;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// `AddSchoolEntryCode` migratsiyasining "bo'sh bo'lmagan production bazasi" yo'li
/// (`PublicSpaceMigrationTests` bilan bir xil naqsh): oraliq nuqtagacha migratsiya → eski
/// sxemaga mos maktablar → qolgan migratsiyalar → backfill natijasi.
///
/// Eng muhim tekshiruv: HAR mavjud maktab (`kind = 1`) UNIKAL, formatga mos kod oladi,
/// ommaviy makon (`kind = 2`) esa `NULL` qoladi.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class EntryCodeMigrationTests(MigrationsPostgresFixture fixture)
{
    /// <summary>`entry_code` HALI YO'Q, lekin `kind` allaqachon bor bo'lgan oxirgi holat.</summary>
    private const string BeforeEntryCodeMigration = "20260905161034_AddPublicSpaceAndPublicUsers";

    /// <summary>`Domain.Schools.SchoolEntryCode.Alphabet` bilan BIR XIL — migratsiya SQL'i shu alifboni ishlatadi.</summary>
    private static readonly Regex EntryCodePattern = new("^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{8}$", RegexOptions.Compiled);

    [DockerFact]
    public async Task MavjudMaktablar_UnikalKodBilanToldiriladi_OmmaviyMakonNullQoladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("entrycodebackfill", ct);

        await MigrationRunner.MigrateToAsync(connectionString, BeforeEntryCodeMigration, ct);
        (await SqlHelpers.CountAsync(connectionString, "information_schema.columns WHERE table_name = 'schools' AND column_name = 'entry_code'", ct))
            .Should().Be(0, "oraliq nuqtada ustun hali yo'q");

        // Eski sxemaga mos ma'lumot: 3 faol maktab, 1 o'chirilgan maktab, 1 ommaviy makon.
        await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind, is_deleted)
            VALUES
                (gen_random_uuid(), '1-maktab', 'Toshkent', 'Chilonzor', 'maktab-1', 'token-1', 1, false),
                (gen_random_uuid(), '2-maktab', 'Toshkent', 'Chilonzor', 'maktab-2', 'token-2', 1, false),
                (gen_random_uuid(), '3-maktab', 'Farg''ona', 'Qo''qon',  'maktab-3', 'token-3', 1, false),
                (gen_random_uuid(), 'Ochirilgan', 'Andijon', 'Asaka',    'maktab-4', 'token-4', 1, true),
                (gen_random_uuid(), 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-5', 2, false);
            """, ct);

        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        // (a) Ustun nullable va DEFAULT'siz — modeldagi bilan bir xil (`SET NOT NULL` ATAYLAB yo'q).
        (await SqlHelpers.ScalarAsync<string>(
                connectionString,
                "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'schools' AND column_name = 'entry_code'",
                ct))
            .Should().Be("YES", "ommaviy makon sababli ustun nullable qoladi");
        (await SqlHelpers.CountAsync(
                connectionString,
                "information_schema.columns WHERE table_name = 'schools' AND column_name = 'entry_code' AND column_default IS NOT NULL",
                ct))
            .Should().Be(0);

        // (b) Barcha `kind = 1` yozuvlar (o'chirilgani ham) kod oldi — formatga mos.
        (await SqlHelpers.CountAsync(connectionString, "schools WHERE kind = 1 AND entry_code IS NULL", ct))
            .Should().Be(0, "har mavjud maktab kod olishi shart");

        var codes = new List<string>();
        foreach (var slug in new[] { "maktab-1", "maktab-2", "maktab-3", "maktab-4" })
        {
            var code = await SqlHelpers.ScalarAsync<string>(connectionString, $"SELECT entry_code FROM schools WHERE slug = '{slug}'", ct);
            code.Should().MatchRegex(EntryCodePattern.ToString(), $"`{slug}` kodi 8 belgi, alifbodan bo'lishi kerak (0/O/1/I/L yo'q)");
            codes.Add(code);
        }

        codes.Distinct().Should().HaveCount(4, "kodlar unikal bo'lishi shart");

        // (c) Ommaviy makon — `NULL` (kod bilan kirish yo'q, faqat Telegram).
        (await SqlHelpers.CountAsync(connectionString, "schools WHERE kind = 2 AND entry_code IS NOT NULL", ct))
            .Should().Be(0);
    }

    [DockerFact]
    public async Task UnikalIndeks_TakrorKodniRadEtadi_NullEsaCheklanmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("entrycodeunique", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind, entry_code)
            VALUES (gen_random_uuid(), '1-maktab', 'Toshkent', 'Chilonzor', 'maktab-1', 'token-1', 1, 'ABCD2345');
            """, ct);

        var duplicate = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind, entry_code)
            VALUES (gen_random_uuid(), '2-maktab', 'Toshkent', 'Chilonzor', 'maktab-2', 'token-2', 1, 'ABCD2345');
            """, ct);

        await duplicate.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "`ux_schools_entry_code` — kod maktabni ANIQLAYDI, takror bo'lmaydi");

        // O'chirilgan maktab kodi ham band qoladi — indeks filtri `is_deleted` ga qaramaydi.
        await SqlHelpers.ExecuteAsync(connectionString, "UPDATE schools SET is_deleted = true WHERE slug = 'maktab-1';", ct);
        await duplicate.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "eski qog'oz/ekranlardagi kod boshqa maktabga olib bormasin");

        // `NULL` (ommaviy makon) unikallikka kirmaydi.
        var nulls = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind, entry_code)
            VALUES (gen_random_uuid(), 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-3', 2, NULL),
                   (gen_random_uuid(), '3-maktab', 'Toshkent', 'Chilonzor', 'maktab-3', 'token-4', 1, NULL);
            """, ct);

        await nulls.Should().NotThrowAsync("qisman indeks (`entry_code IS NOT NULL`) `NULL` larni cheklamaydi");
    }
}

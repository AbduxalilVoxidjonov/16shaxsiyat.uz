using FluentAssertions;
using Npgsql;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// `admin_users.concurrency_stamp` — optimistik konkurentlik tokeni. Uni HAR QATOR uchun
/// noyob bo'lishi va DOIMIY `DEFAULT`ga ega BO'LMASLIGI shart.
///
/// Nima uchun alohida sinov qatlami: `20260902070721_AddAuditLogsAndTotpSupport` mavjud
/// jadvalga `NOT NULL uuid` ustun qo'shganda EF Core `defaultValue: Guid.Empty` yozgan va
/// buni ustunning DOIMIY `DEFAULT`i sifatida chiqargan. Barcha boshqa testlar
/// `EnsureCreated()` (modeldan qurilgan sxema) ustida ishlaydi — u yerda bunday `DEFAULT`
/// YO'Q, ya'ni bu farqni FAQAT haqiqiy migratsiya yo'li ko'ra oladi (`docs/13` §8.1).
/// Tuzatish: `20260902194217_DropConcurrencyStampDefault`.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class ConcurrencyStampDefaultTests(MigrationsPostgresFixture fixture)
{
    /// <summary>`concurrency_stamp` ustuni NUQSONLI `DEFAULT` bilan qo'shilgan nuqta.</summary>
    private const string ColumnAddedMigration = "20260902070721_AddAuditLogsAndTotpSupport";

    private const string ZeroGuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>`information_schema` da `column_default` yo'qligi (`NULL`) shu belgi bilan qaytadi.</summary>
    private const string NoDefault = "-";

    [DockerFact]
    public async Task BarchaMigratsiyalardanKeyin_ConcurrencyStampda_DoimiyDefaultQolmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("stampdefault", ct);

        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        (await ColumnDefaultAsync(connectionString, ct)).Should().Be(NoDefault,
            "`concurrency_stamp` — konkurentlik tokeni; DOIMIY DEFAULT bo'lsa qiymatsiz "
            + "INSERT jimgina nol-GUID yozadi va token o'z vazifasini bajarmaydi");

        (await ZeroStampCountAsync(connectionString, ct)).Should().Be(0,
            "nol-GUID hech qachon haqiqiy konkurentlik tokeni emas");

        // Default yo'qligining AMALIY isboti: qiymatsiz INSERT endi jimgina o'tmaydi,
        // 23502 (not-null violation) bilan RAD ETILADI.
        var act = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO admin_users (username, email, password_hash)
            VALUES ('stampless', 'stampless@example.uz', 'hash');
            """, ct);

        await act.Should().ThrowAsync<PostgresException>()
            .Where(e => e.SqlState == "23502",
                "DEFAULT olib tashlangach `concurrency_stamp`siz INSERT rad etilishi kerak");
    }

    /// <summary>
    /// BO'SH BO'LMAGAN production bazasi yo'li: nuqsonli `DEFAULT` amal qilgan davrda
    /// yozilgan (ya'ni BIR XIL nol stamp olgan) adminlar migratsiyadan keyin HAR BIRI
    /// NOYOB qiymat olishi shart — aks holda ular orasida optimistik konkurentlik
    /// tekshiruvi ishlamay qolaveradi.
    /// </summary>
    [DockerFact]
    public async Task NolGuidStampliMavjudAdminlar_Migratsiyadan_Keyin_NoyobQiymatOladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("stampbackfill", ct);

        // 1) ORALIQ NUQTA: ustun endigina qo'shilgan — nuqsonli DEFAULT hali kuchda.
        await MigrationRunner.MigrateToAsync(connectionString, ColumnAddedMigration, ct);

        (await ColumnDefaultAsync(connectionString, ct)).Should().Be($"'{ZeroGuid}'::uuid",
            "oraliq nuqtada aynan shu nuqsonli DEFAULT mavjud bo'lishi kerak — bo'lmasa bu test eskirgan");

        // 2) O'sha sxemaga mos ma'lumot: uch admin `concurrency_stamp`SIZ yoziladi
        //    (production'dagi migratsiya/xom SQL yo'li aynan shunday) — hammasi nol stamp oladi.
        //    To'rtinchisi HAQIQIY (nol bo'lmagan) stamp bilan — u tegilmasligi kerak.
        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO admin_users (username, email, password_hash)
            VALUES
                ('eski1', 'eski1@example.uz', 'hash1'),
                ('eski2', 'eski2@example.uz', 'hash2'),
                ('eski3', 'eski3@example.uz', 'hash3');

            INSERT INTO admin_users (username, email, password_hash, concurrency_stamp)
            VALUES ('eski4', 'eski4@example.uz', 'hash4', '{HealthyStamp}'::uuid);
            """, ct);

        (await ZeroStampCountAsync(connectionString, ct)).Should().Be(3,
            "nuqsonli DEFAULT tufayli qiymatsiz yozilgan uch admin ham NOL stamp olishi kerak (muammoning o'zi)");
        (await DistinctStampCountAsync(connectionString, ct)).Should().Be(2,
            "tuzatishdan OLDIN uch admin BIR XIL stamp bilan qolgan — ya'ni 4 qator, atigi 2 xil qiymat");

        // 3) Qolgan migratsiyalar — production'dagi `--migrate` kabi bitta chaqiruvda.
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        // 4) Natija.
        (await SqlHelpers.CountAsync(connectionString, "admin_users", ct)).Should().Be(4,
            "migratsiya hech bir adminni o'chirmasligi kerak (destruktiv emas)");

        (await ZeroStampCountAsync(connectionString, ct)).Should().Be(0,
            "nol-GUID stamp'li qator qolmasligi shart");

        (await DistinctStampCountAsync(connectionString, ct)).Should().Be(4,
            "har bir admin NOYOB stamp olishi shart — bir xil qiymat konkurentlik himoyasini o'ldiradi");

        (await SqlHelpers.CountAsync(connectionString, $"admin_users WHERE concurrency_stamp = '{HealthyStamp}'::uuid", ct))
            .Should().Be(1, "allaqachon to'g'ri bo'lgan stamp o'zgartirilmasligi kerak");

        (await ColumnDefaultAsync(connectionString, ct)).Should().Be(NoDefault,
            "migratsiya DEFAULT'ni ham olib tashlashi shart — aks holda muammo qayta paydo bo'ladi");
    }

    private static readonly Guid HealthyStamp = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static async Task<string> ColumnDefaultAsync(string connectionString, CancellationToken ct)
        => await SqlHelpers.ScalarAsync<string>(
            connectionString,
            $"""
             SELECT COALESCE(column_default, '{NoDefault}')
             FROM information_schema.columns
             WHERE table_schema = 'public'
               AND table_name = 'admin_users'
               AND column_name = 'concurrency_stamp'
             """,
            ct);

    private static async Task<long> ZeroStampCountAsync(string connectionString, CancellationToken ct)
        => await SqlHelpers.CountAsync(connectionString, $"admin_users WHERE concurrency_stamp = '{ZeroGuid}'::uuid", ct);

    private static async Task<long> DistinctStampCountAsync(string connectionString, CancellationToken ct)
        => await SqlHelpers.ScalarAsync<long>(connectionString, "SELECT count(DISTINCT concurrency_stamp) FROM admin_users;", ct);
}

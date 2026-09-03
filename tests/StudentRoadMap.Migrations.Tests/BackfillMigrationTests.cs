using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// ⚠️ ENG MUHIM SSENARIY (`docs/13` §8, P34 QA bloklovchi topilmasi).
///
/// Migratsiyalar ORALIQ NUQTAGACHA qo'llanadi, o'sha (eski) sxemaga mos ma'lumot yoziladi,
/// keyin qolgan migratsiyalar qo'llanadi. Ya'ni bu — "BO'SH BO'LMAGAN production bazasi"
/// yo'li. Aynan shu yo'lda 2026-09-02 da deploy to'xtagan edi: mavjud `assessments`
/// qatorlari `program_id`siz qolib `SET NOT NULL` / FK cheklovini buzardi, va HECH BIR
/// TEST buni ushlay olmasdi (barcha integratsiya testlari `EnsureCreated()` ishlatardi —
/// migratsiyalar umuman bajarilmasdi).
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class BackfillMigrationTests(MigrationsPostgresFixture fixture)
{
    /// <summary>`program_id` ustuni HALI YO'Q bo'lgan holat — P34 dagi haqiqiy boshlang'ich nuqta.</summary>
    private const string BeforeProgramsMigration = "20260902070721_AddAuditLogsAndTotpSupport";

    /// <summary>`program_id` qo'shilgan, lekin hali `NULL` bo'lishi mumkin bo'lgan oraliq holat.</summary>
    private const string NullableProgramIdMigration = "20260902123104_AddAssessmentPrograms";

    /// <summary>`RequireAssessmentProgramId` migratsiyasidagi deterministik tizim dasturi ID'si.</summary>
    private static readonly Guid SystemProgramId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private const string SystemProgramCode = "PERSONALITY_PROFILE";

    [DockerFact]
    public async Task ProgramIdUstuniYoq_HolatidanKochirish_MavjudSessiyalarniTizimDasturigaBoglaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("backfillold", ct);

        // 1) ORALIQ NUQTAGACHA migratsiya — `assessments` da `program_id` ustuni hali yo'q.
        await MigrationRunner.MigrateToAsync(connectionString, BeforeProgramsMigration, ct);
        await AssertColumnExistsAsync(connectionString, "assessments", "program_id", shouldExist: false, ct);

        // 2) O'SHA SXEMAGA MOS ma'lumot: 1 maktab, 2 o'quvchi, 3 sessiya + 4 tizim metodikasi.
        await SeedLegacyDataAsync(connectionString, assessmentCount: 3, ct);

        // 3) Qolgan migratsiyalar — production'dagi `--migrate` kabi bitta chaqiruvda.
        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        // 4) Backfill natijasi.
        await AssertBackfillCompletedAsync(connectionString, expectedAssessments: 3, ct);
    }

    [DockerFact]
    public async Task ProgramIdNull_HolatidanKochirish_SetNotNullBuzilmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("backfillnull", ct);

        // Aynan yiqilgan qadamdan OLDINGI holat: ustun bor, lekin qiymatlar `NULL`.
        await MigrationRunner.MigrateToAsync(connectionString, NullableProgramIdMigration, ct);
        await AssertColumnExistsAsync(connectionString, "assessments", "program_id", shouldExist: true, ct);

        await SeedLegacyDataAsync(connectionString, assessmentCount: 5, ct);

        var nullCount = await SqlHelpers.CountAsync(connectionString, "assessments WHERE program_id IS NULL", ct);
        nullCount.Should().Be(5, "oraliq holatda barcha sessiyalar `program_id`siz bo'lishi kerak");

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            // `RequireAssessmentProgramId` shu yerda ishlaydi: backfill → keyin `SET NOT NULL`.
            // Backfill bo'lmasa bu chaqiruv 23502/23503 bilan yiqilardi (P34).
            await context.Database.MigrateAsync(ct);
        }

        await AssertBackfillCompletedAsync(connectionString, expectedAssessments: 5, ct);
    }

    /// <summary>
    /// Nol-GUID / "soxta" dastur qoldirilmaganini alohida tasdiqlaydi — P34 da aynan
    /// mavjud sessiyalar NOL-GUID ga bog'lanib FK cheklovini buzardi.
    /// </summary>
    [DockerFact]
    public async Task Backfilldan_Keyin_YetimQolganYokiNolGuidli_SessiyaQolmaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("backfillfk", ct);

        await MigrationRunner.MigrateToAsync(connectionString, BeforeProgramsMigration, ct);
        await SeedLegacyDataAsync(connectionString, assessmentCount: 4, ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        var orphans = await SqlHelpers.CountAsync(
            connectionString,
            "assessments a LEFT JOIN assessment_programs p ON a.program_id = p.id WHERE p.id IS NULL",
            ct);
        orphans.Should().Be(0, "hech bir sessiya mavjud bo'lmagan dasturga (masalan nol-GUID) bog'lanmasligi kerak");

        var zeroGuid = await SqlHelpers.CountAsync(
            connectionString,
            "assessments WHERE program_id = '00000000-0000-0000-0000-000000000000'::uuid",
            ct);
        zeroGuid.Should().Be(0, "nol-GUID hech qachon haqiqiy dastur identifikatori emas");

        // FK haqiqatan ham kuchda: mavjud bo'lmagan dasturga yozishga urinish rad etilishi shart.
        var act = async () => await SqlHelpers.ExecuteAsync(
            connectionString,
            "UPDATE assessments SET program_id = '00000000-0000-0000-0000-0000000000ff'::uuid;",
            ct);

        await act.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23503", "chet el kaliti cheklovi (FK) kuchda bo'lishi kerak");
    }

    /// <summary>
    /// Migratsiya → seed TOPSHIRIG'I (`docker-compose.yml`: migrate → seed → api). Yangi
    /// o'rnatishda migratsiya tizim dasturini BO'SH tarkib bilan yaratadi (test banki hali
    /// seed qilinmagan); keyin `DbSeeder` haqiqiy 4 metodikani bog'lashi va DUBLIKAT dastur
    /// yaratmasligi shart — ID deterministik bo'lgani uchun (`DbSeeder.
    /// SystemPersonalityProfileProgramId` == migratsiyadagi konstanta).
    /// </summary>
    [DockerFact]
    public async Task YangiOrnatishda_MigratsiyaBoshDasturYaratadi_SeedEsaTestlarniBoglaydi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("migratethenseed", ct);

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await context.Database.MigrateAsync(ct);
        }

        // Migratsiya dasturni yaratdi, lekin test banki hali yo'q — tarkib bo'sh.
        (await SqlHelpers.CountAsync(connectionString, $"assessment_programs WHERE code = '{SystemProgramCode}'", ct))
            .Should().Be(1);
        (await SqlHelpers.CountAsync(connectionString, "program_tests", ct))
            .Should().Be(0, "test banki hali seed qilinmagan — migratsiya 0 ta testni bog'laydi");

        await using (var context = MigrationsPostgresFixture.CreateContext(connectionString))
        {
            await SeedRunner.Create(context).SeedAsync(ct);
        }

        (await SqlHelpers.CountAsync(connectionString, $"assessment_programs WHERE code = '{SystemProgramCode}'", ct))
            .Should().Be(1, "seed DUBLIKAT dastur yaratmasligi shart (deterministik ID)");

        (await SqlHelpers.ScalarAsync<Guid>(connectionString, $"SELECT id FROM assessment_programs WHERE code = '{SystemProgramCode}'", ct))
            .Should().Be(SystemProgramId, "migratsiya va seeder AYNAN bir xil ID ishlatishi shart");

        (await SqlHelpers.CountAsync(connectionString, $"program_tests WHERE program_id = '{SystemProgramId}'::uuid", ct))
            .Should().Be(4, "seed 4 tizim metodikasini tizim dasturiga bog'lashi kerak");
    }

    // ---------------------------------------------------------------------------------------
    // Yordamchilar
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// ESKI sxemaga mos ma'lumot (faqat o'sha paytda mavjud bo'lgan ustunlar bilan!):
    /// 1 maktab → 2 o'quvchi → N sessiya, hamda 4 tizim metodikasi.
    /// `program_id` ATAYLAB berilmaydi — mavjud production qatorlari aynan shunday.
    /// </summary>
    private static async Task SeedLegacyDataAsync(string connectionString, int assessmentCount, CancellationToken ct)
    {
        var assessmentRows = string.Join(
            ",\n",
            Enumerable.Range(1, assessmentCount).Select(i => $"""
                ('{Guid.NewGuid()}'::uuid,
                 (SELECT id FROM students ORDER BY full_name LIMIT 1 OFFSET {i % 2}),
                 '{SchoolId}'::uuid,
                 'legacy-session-token-{i:D4}',
                 now() + interval '7 days')
                """));

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token)
            VALUES ('{SchoolId}'::uuid, 'Eski maktab', 'Toshkent', 'Yunusobod', 'eski-maktab', 'legacy-access-token-0001');

            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES
                ('{StudentAId}'::uuid, '{SchoolId}'::uuid, 'Ali Valiyev', 'ali valiyev', DATE '2010-05-14', 9, '+998901112233', now()),
                ('{StudentBId}'::uuid, '{SchoolId}'::uuid, 'Zilola Karimova', 'zilola karimova', DATE '2011-02-03', 8, '+998901112244', now());

            INSERT INTO test_definitions (id, code, name_uz, display_order, estimated_minutes, scoring_strategy, is_system)
            VALUES
                (gen_random_uuid(), 'MBTI16',   'Shaxsiyat tipi',      1, 15, 'Dichotomy', true),
                (gen_random_uuid(), 'BIG5',     'Big Five',            2, 12, 'Likert',    true),
                (gen_random_uuid(), 'RIASEC',   'Kasb qiziqishlari',   3, 10, 'Likert',    true),
                (gen_random_uuid(), 'ACTIVITY', 'Aktivlik',            4,  8, 'Likert',    true);

            INSERT INTO assessments (id, student_id, school_id, session_token, expires_at)
            VALUES
            {assessmentRows};
            """, ct);
    }

    private static async Task AssertBackfillCompletedAsync(string connectionString, long expectedAssessments, CancellationToken ct)
    {
        // (a) Ustun endi NOT NULL.
        var isNullable = await SqlHelpers.ScalarAsync<string>(
            connectionString,
            "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'assessments' AND column_name = 'program_id'",
            ct);
        isNullable.Should().Be("NO", "`RequireAssessmentProgramId` ustunni NOT NULL qilishi kerak");

        // (b) Barcha eski sessiyalar tizim dasturiga bog'landi.
        (await SqlHelpers.CountAsync(connectionString, "assessments", ct))
            .Should().Be(expectedAssessments, "migratsiya hech bir sessiyani o'chirmasligi kerak");
        (await SqlHelpers.CountAsync(connectionString, $"assessments WHERE program_id = '{SystemProgramId}'::uuid", ct))
            .Should().Be(expectedAssessments, "mavjud sessiyalar tizim dasturiga backfill qilinishi shart");

        // (c) Tizim dasturi bitta, deterministik ID bilan.
        (await SqlHelpers.CountAsync(connectionString, "assessment_programs", ct)).Should().Be(1);
        (await SqlHelpers.ScalarAsync<Guid>(connectionString, $"SELECT id FROM assessment_programs WHERE code = '{SystemProgramCode}'", ct))
            .Should().Be(SystemProgramId);
        (await SqlHelpers.ScalarAsync<bool>(connectionString, $"SELECT is_system FROM assessment_programs WHERE code = '{SystemProgramCode}'", ct))
            .Should().BeTrue();

        // (d) Mavjud 4 metodika dasturga bog'landi (migratsiya ichidagi 2-qadam).
        (await SqlHelpers.CountAsync(connectionString, $"program_tests WHERE program_id = '{SystemProgramId}'::uuid", ct))
            .Should().Be(4, "eski bazada test banki allaqachon mavjud edi — migratsiya ularni bog'lashi kerak");

        // (e) FK butunligi buzilmagan.
        (await SqlHelpers.CountAsync(
            connectionString,
            "assessments a LEFT JOIN assessment_programs p ON a.program_id = p.id WHERE p.id IS NULL",
            ct)).Should().Be(0);
    }

    private static async Task AssertColumnExistsAsync(string connectionString, string table, string column, bool shouldExist, CancellationToken ct)
    {
        var count = await SqlHelpers.CountAsync(
            connectionString,
            $"information_schema.columns WHERE table_name = '{table}' AND column_name = '{column}'",
            ct);

        count.Should().Be(shouldExist ? 1 : 0,
            $"`{table}.{column}` ustuni oraliq nuqtada {(shouldExist ? "mavjud" : "mavjud emas")} bo'lishi kerak");
    }

    private static readonly Guid SchoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StudentAId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StudentBId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}

using FluentAssertions;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// P47 — `AddPublicSpaceAndPublicUsers` migratsiyasining "bo'sh bo'lmagan production bazasi"
/// yo'li (`BackfillMigrationTests` bilan bir xil naqsh): oraliq nuqtagacha migratsiya →
/// eski sxemaga mos ma'lumot → qolgan migratsiyalar → natijani tekshirish.
///
/// Eng muhim tekshiruv — `assessments.session_token_hash` backfill'i: mavjud FAOL sessiyalar
/// buzilmasligi kerak (xesh bazada, ochiq matnli tokendan hisoblanadi).
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class PublicSpaceMigrationTests(MigrationsPostgresFixture fixture)
{
    /// <summary>`kind`/`session_token_hash`/`public_users` HALI YO'Q bo'lgan oxirgi holat.</summary>
    private const string BeforePublicSpaceMigration = "20260905120119_AddPendingTotpEnrollment";

    private static readonly Guid SchoolId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid StudentId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    /// <summary>SHA-256("legacy-session-token-0001") — `Domain.Common.TokenHash.Compute` bilan bir xil.</summary>
    private const string LegacySessionToken = "legacy-session-token-0001";

    [DockerFact]
    public async Task MavjudSessiyalar_SessionTokenHashBilanBackfillQilinadi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspacebackfill", ct);

        await MigrationRunner.MigrateToAsync(connectionString, BeforePublicSpaceMigration, ct);
        await AssertColumnExistsAsync(connectionString, "assessments", "session_token_hash", shouldExist: false, ct);

        await SeedLegacyDataAsync(connectionString, ct);

        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        // (a) Ustun bor va NOT NULL.
        await AssertColumnExistsAsync(connectionString, "assessments", "session_token_hash", shouldExist: true, ct);
        (await SqlHelpers.ScalarAsync<string>(
                connectionString,
                "SELECT is_nullable FROM information_schema.columns WHERE table_name = 'assessments' AND column_name = 'session_token_hash'",
                ct))
            .Should().Be("NO");

        // (b) Ustunda DOIMIY `DEFAULT` QOLMAGAN — aks holda modeldan qurilgan sxemadan farq
        //     qilardi (loyihadagi `admin_users.concurrency_stamp` texnik qarzi kabi).
        (await SqlHelpers.CountAsync(
                connectionString,
                "information_schema.columns WHERE table_name = 'assessments' AND column_name = 'session_token_hash' AND column_default IS NOT NULL",
                ct))
            .Should().Be(0);

        // (c) Xesh AYNAN SHA-256(hex, kichik harf) — ya'ni mavjud sessiya tokeni bilan
        //     autentifikatsiya qilish hali ham mumkin (hech kim bazadan chiqarib tashlanmadi).
        var storedHash = await SqlHelpers.ScalarAsync<string>(
            connectionString,
            $"SELECT session_token_hash FROM assessments WHERE session_token = '{LegacySessionToken}'",
            ct);

        var expectedHash = await SqlHelpers.ScalarAsync<string>(
            connectionString,
            $"SELECT encode(sha256(convert_to('{LegacySessionToken}', 'UTF8')), 'hex')",
            ct);

        storedHash.Should().Be(expectedHash);
        storedHash.Should().HaveLength(64);

        // (d) Bo'sh yoki takrorlangan xesh qolmadi (unikal indeks buzilmasligi shart).
        (await SqlHelpers.CountAsync(connectionString, "assessments WHERE session_token_hash = ''", ct)).Should().Be(0);
        (await SqlHelpers.CountAsync(connectionString, "assessments", ct)).Should().Be(3, "migratsiya hech bir sessiyani o'chirmasligi kerak");
    }

    [DockerFact]
    public async Task MavjudMaktablar_KindVaShowResultToStudentBilanBackfillQilinadi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspaceschools", ct);

        await MigrationRunner.MigrateToAsync(connectionString, BeforePublicSpaceMigration, ct);
        await SeedLegacyDataAsync(connectionString, ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        (await SqlHelpers.ScalarAsync<short>(connectionString, $"SELECT kind FROM schools WHERE id = '{SchoolId}'::uuid", ct))
            .Should().Be(1, "mavjud yozuvlar haqiqiy maktab (`SchoolKind.School`)");

        (await SqlHelpers.ScalarAsync<bool>(connectionString, $"SELECT show_result_to_student FROM schools WHERE id = '{SchoolId}'::uuid", ct))
            .Should().BeFalse("eski global `App:ShowResultToStudent` standarti `false` edi");

        // `kind` ustunida DB DEFAULT qolmasligi kerak (model bilan mos).
        (await SqlHelpers.CountAsync(
                connectionString,
                "information_schema.columns WHERE table_name = 'schools' AND column_name = 'kind' AND column_default IS NOT NULL",
                ct))
            .Should().Be(0);
    }

    [DockerFact]
    public async Task IkkinchiOmmaviyMakon_UnikalIndeksBilanRadEtiladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspaceunique", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES (gen_random_uuid(), 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-1', 2);
            """, ct);

        var act = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES (gen_random_uuid(), 'Ikkinchi makon', 'Ommaviy', 'Ommaviy', 'ommaviy-2', 'token-2', 2);
            """, ct);

        await act.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "`ux_schools_public_space` bazada AYNAN BITTA ommaviy makonni kafolatlaydi");

        // Oddiy maktablar esa cheklanmaydi — indeks faqat `kind = 2` ga tegishli.
        var addSchools = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES (gen_random_uuid(), '1-maktab', 'Toshkent', 'Chilonzor', 'maktab-1', 'token-3', 1),
                   (gen_random_uuid(), '2-maktab', 'Toshkent', 'Chilonzor', 'maktab-2', 'token-4', 1);
            """, ct);

        await addSchools.Should().NotThrowAsync();
    }

    [DockerFact]
    public async Task OmmaviyMakonda_BirXilIsmVaTugilganSana_IkkiXilAkkauntUchunRuxsatEtiladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspaceidentity", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES ('{SchoolId}'::uuid, 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-1', 2);

            INSERT INTO public_users (id, telegram_id, last_login_at)
            VALUES ('66666666-6666-6666-6666-666666666666'::uuid, 111111111, now()),
                   ('77777777-7777-7777-7777-777777777777'::uuid, 222222222, now());
            """, ct);

        // Ikki XIL Telegram akkaunti, bir xil F.I.Sh. + tug'ilgan sana — real hayotda uchraydi.
        var act = async () => await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO students (id, school_id, public_user_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES
                (gen_random_uuid(), '{SchoolId}'::uuid, '66666666-6666-6666-6666-666666666666'::uuid,
                 'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112233', now()),
                (gen_random_uuid(), '{SchoolId}'::uuid, '77777777-7777-7777-7777-777777777777'::uuid,
                 'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112244', now());
            """, ct);

        await act.Should().NotThrowAsync("`ux_students_identity` endi faqat maktab oqimiga (`public_user_id IS NULL`) tegishli");

        // Maktab oqimida esa eski qoida AYNAN o'z kuchida qoladi.
        await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES ('88888888-8888-8888-8888-888888888888'::uuid, '1-maktab', 'Toshkent', 'Chilonzor', 'maktab-1', 'token-3', 1);

            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES (gen_random_uuid(), '88888888-8888-8888-8888-888888888888'::uuid,
                    'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112255', now());
            """, ct);

        var duplicateInSchool = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES (gen_random_uuid(), '88888888-8888-8888-8888-888888888888'::uuid,
                    'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112266', now());
            """, ct);

        await duplicateInSchool.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "maktab ichida F.I.Sh.+tug'ilgan sana unikalligi o'zgarmaydi");
    }

    [DockerFact]
    public async Task BittaAkkauntgaBittaOquvchiProfili_UnikalIndeksBilanQulflanadi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspaceoneprofile", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES ('{SchoolId}'::uuid, 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-1', 2);

            INSERT INTO public_users (id, telegram_id, last_login_at)
            VALUES ('66666666-6666-6666-6666-666666666666'::uuid, 111111111, now());

            INSERT INTO students (id, school_id, public_user_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES (gen_random_uuid(), '{SchoolId}'::uuid, '66666666-6666-6666-6666-666666666666'::uuid,
                    'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112233', now());
            """, ct);

        var second = async () => await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO students (id, school_id, public_user_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES (gen_random_uuid(), '{SchoolId}'::uuid, '66666666-6666-6666-6666-666666666666'::uuid,
                    'Boshqa Ism', 'BOSHQA ISM', DATE '2011-05-14', 8, '+998901112244', now());
            """, ct);

        await second.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "`ux_students_public_user`: bitta akkauntga bitta profil — 90 kunlik oyna shu profil bo'yicha ishlaydi");
    }

    [DockerFact]
    public async Task TelegramIdUnikal_LekinOchirilganAkkauntQaytaRoyxatdanOtaOladi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspacetelegram", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO public_users (id, telegram_id, last_login_at)
            VALUES ('66666666-6666-6666-6666-666666666666'::uuid, 111111111, now());
            """, ct);

        var duplicate = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO public_users (id, telegram_id, last_login_at)
            VALUES (gen_random_uuid(), 111111111, now());
            """, ct);

        await duplicate.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23505", "`ux_public_users_telegram` bir xil Telegram ID bilan ikkita akkauntni to'xtatadi");

        // Anonimlashtirish (`PublicUser.MarkDeleted`) — `telegram_id = NULL`; keyin qayta kirish mumkin.
        await SqlHelpers.ExecuteAsync(connectionString, """
            UPDATE public_users SET telegram_id = NULL, username = NULL, first_name = NULL,
                                    last_name = NULL, photo_url = NULL, deleted_at = now()
            WHERE id = '66666666-6666-6666-6666-666666666666'::uuid;
            """, ct);

        var afterDeletion = async () => await SqlHelpers.ExecuteAsync(connectionString, """
            INSERT INTO public_users (id, telegram_id, last_login_at)
            VALUES (gen_random_uuid(), 111111111, now());
            """, ct);

        await afterDeletion.Should().NotThrowAsync("qisman indeks (`telegram_id IS NOT NULL`) o'chirilgan akkauntni bloklamaydi");
    }

    [DockerFact]
    public async Task SinfCheklovi_NolgaRuxsatBeradi_LekinManfiyVaOnIkkigaYoq()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("pubspacegrade", ct);
        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES ('{SchoolId}'::uuid, 'Ommaviy makon', 'Ommaviy', 'Ommaviy', 'ommaviy', 'token-1', 2);
            """, ct);

        // `Student.NoGrade` (0) — maktabda o'qimaydigan kattalar uchun.
        var zeroGrade = async () => await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES ('{StudentId}'::uuid, '{SchoolId}'::uuid, 'Katta Odam', 'KATTA ODAM', DATE '1996-05-14', 0, '+998901112233', now());
            """, ct);

        await zeroGrade.Should().NotThrowAsync("`ck_students_grade` endi `0..11`");

        var invalidGrade = async () => await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES (gen_random_uuid(), '{SchoolId}'::uuid, 'Xato Sinf', 'XATO SINF', DATE '2010-05-14', 12, '+998901112244', now());
            """, ct);

        await invalidGrade.Should().ThrowAsync<Npgsql.PostgresException>()
            .Where(e => e.SqlState == "23514", "12-sinf hech qachon yaroqli emas");
    }

    // ---------------------------------------------------------------------------------------
    // Yordamchilar
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// P47 dan OLDINGI sxemaga mos ma'lumot: 1 maktab, 1 o'quvchi, 3 sessiya (ochiq matnli
    /// `session_token` bilan) va 1 dastur. `kind`/`public_user_id`/`session_token_hash`
    /// ustunlari bu nuqtada HALI YO'Q.
    /// </summary>
    private static async Task SeedLegacyDataAsync(string connectionString, CancellationToken ct)
    {
        var programId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token)
            VALUES ('{SchoolId}'::uuid, 'Eski maktab', 'Toshkent', 'Yunusobod', 'eski-maktab', 'legacy-access-token-0002');

            INSERT INTO students (id, school_id, full_name, normalized_name, birth_date, grade, phone, consent_given_at)
            VALUES ('{StudentId}'::uuid, '{SchoolId}'::uuid, 'Ali Valiyev', 'ALI VALIYEV', DATE '2010-05-14', 9, '+998901112233', now());

            INSERT INTO assessment_programs (id, code, name_uz, display_order, kind, visibility, status, is_system, is_active)
            VALUES ('{programId}'::uuid, 'PERSONALITY_PROFILE', 'Shaxsiyat profili', 1, 1, 1, 2, true, true)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO assessments (id, student_id, school_id, program_id, session_token, expires_at)
            VALUES
                (gen_random_uuid(), '{StudentId}'::uuid, '{SchoolId}'::uuid, '{programId}'::uuid, '{LegacySessionToken}', now() + interval '7 days'),
                (gen_random_uuid(), '{StudentId}'::uuid, '{SchoolId}'::uuid, '{programId}'::uuid, 'legacy-session-token-0002', now() + interval '7 days'),
                (gen_random_uuid(), '{StudentId}'::uuid, '{SchoolId}'::uuid, '{programId}'::uuid, 'legacy-session-token-0003', now() + interval '7 days');
            """, ct);
    }

    private static async Task AssertColumnExistsAsync(string connectionString, string table, string column, bool shouldExist, CancellationToken ct)
    {
        var count = await SqlHelpers.CountAsync(
            connectionString,
            $"information_schema.columns WHERE table_name = '{table}' AND column_name = '{column}'",
            ct);

        count.Should().Be(shouldExist ? 1 : 0,
            $"`{table}.{column}` ustuni {(shouldExist ? "mavjud" : "mavjud emas")} bo'lishi kerak");
    }
}

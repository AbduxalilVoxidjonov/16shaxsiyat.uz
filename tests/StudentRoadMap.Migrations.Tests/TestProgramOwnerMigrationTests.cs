using FluentAssertions;
using StudentRoadMap.Infrastructure.Migrations;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// 2026-09-23 egasi qarori (`docs/18` §9.7) — `AddTestProgramOwner` migratsiyasining "bo'sh
/// bo'lmagan production bazasi" yo'li (`BackfillMigrationTests` naqshi): oraliq nuqtagacha
/// migratsiya → jonli bazaga o'xshash ma'lumot → qolgan migratsiyalar → natija.
///
/// Jonli bazadagi holat nusxasi: `FORMS` (Custom, nashr qilingan, faol, faqat
/// `INTELLECT-SURVEY`, 2 maktab, 4 sessiya) → O'SHA testning test dasturiga aylanishi
/// (yangisi yaratilmasdan), `PERSONALITY_PROFILE` (ko'p testli, arxiv) va `1` (arxiv, 0 test)
/// — tegilmasligi.
/// </summary>
[Collection(MigrationsPostgresCollection.Name)]
[Trait("Category", "Migrations")]
public sealed class TestProgramOwnerMigrationTests(MigrationsPostgresFixture fixture)
{
    /// <summary>`owner_test_definition_id` HALI YO'Q bo'lgan oxirgi holat.</summary>
    private const string BeforeTestProgramOwnerMigration = "20260911234638_AddStudentProfileExtra";

    /// <summary>`RequireAssessmentProgramId` migratsiyasidagi deterministik tizim dasturi ID'si.</summary>
    private static readonly Guid SystemProgramId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly Guid SchoolAId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid SchoolBId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    private static readonly Guid StudentId = Guid.Parse("a2000000-0000-0000-0000-000000000001");

    private static readonly Guid IntellectTestId = Guid.Parse("b1000000-0000-0000-0000-000000000001");
    private static readonly Guid MbtiTestId = Guid.Parse("b1000000-0000-0000-0000-000000000002");
    private static readonly Guid Big5TestId = Guid.Parse("b1000000-0000-0000-0000-000000000003");
    private static readonly Guid ArchivedSoloTestId = Guid.Parse("b1000000-0000-0000-0000-000000000004");
    private static readonly Guid DraftTestId = Guid.Parse("b1000000-0000-0000-0000-000000000005");

    private static readonly Guid FormsProgramId = Guid.Parse("c1000000-0000-0000-0000-000000000001");
    private static readonly Guid LaterFormsCopyProgramId = Guid.Parse("c1000000-0000-0000-0000-000000000002");
    private static readonly Guid EmptyArchivedProgramId = Guid.Parse("c1000000-0000-0000-0000-000000000003");
    private static readonly Guid ArchivedSoloProgramId = Guid.Parse("c1000000-0000-0000-0000-000000000004");
    private static readonly Guid DraftTestProgramId = Guid.Parse("c1000000-0000-0000-0000-000000000005");

    [DockerFact]
    public async Task FormsDasturi_IntellectSurveyTestDasturigaAylanadi_BiriktirmaVaSessiyalarSaqlanadi()
    {
        var ct = CancellationToken.None;
        var connectionString = await fixture.CreateEmptyDatabaseAsync("testprogramowner", ct);

        await MigrationRunner.MigrateToAsync(connectionString, BeforeTestProgramOwnerMigration, ct);
        await SeedLiveLikeDataAsync(connectionString, ct);

        var programCountBefore = await SqlHelpers.CountAsync(connectionString, "assessment_programs", ct);

        await MigrationRunner.MigrateAllAsync(connectionString, ct);

        // (a) Yangi dastur YARATILMADI — mavjudlari belgilandi.
        (await SqlHelpers.CountAsync(connectionString, "assessment_programs", ct))
            .Should().Be(programCountBefore, "ma'lumot migratsiyasi yangi dastur yaratmasligi kerak");

        // (b) FORMS → INTELLECT-SURVEY test dasturi; nomi/kodi/tavsifi testdan.
        (await SqlHelpers.ScalarAsync<Guid>(connectionString, $"SELECT owner_test_definition_id FROM assessment_programs WHERE id = '{FormsProgramId}'", ct))
            .Should().Be(IntellectTestId);
        (await SqlHelpers.ScalarAsync<string>(connectionString, $"SELECT code FROM assessment_programs WHERE id = '{FormsProgramId}'", ct))
            .Should().Be("INTELLECT-SURVEY");
        (await SqlHelpers.ScalarAsync<string>(connectionString, $"SELECT name_uz FROM assessment_programs WHERE id = '{FormsProgramId}'", ct))
            .Should().Be("Intellekt so'rovnomasi");
        (await SqlHelpers.ScalarAsync<string>(connectionString, $"SELECT description_uz FROM assessment_programs WHERE id = '{FormsProgramId}'", ct))
            .Should().Be("Test tavsifi");
        (await SqlHelpers.ScalarAsync<bool>(connectionString, $"SELECT is_active FROM assessment_programs WHERE id = '{FormsProgramId}'", ct))
            .Should().BeTrue("test nashr qilingan va faol — dastur ochiq qoladi");

        // (c) 2 maktab biriktirmasi va 4 sessiya saqlandi.
        (await SqlHelpers.CountAsync(connectionString, $"school_programs WHERE program_id = '{FormsProgramId}'", ct)).Should().Be(2);
        (await SqlHelpers.CountAsync(connectionString, $"assessments WHERE program_id = '{FormsProgramId}'", ct)).Should().Be(4);

        // (d) Tegilmaydiganlar: ko'p testli tizim dasturi, bo'sh arxiv, arxivlangan bir testli,
        //     va shu testga KEYINROQ yaratilgan ikkinchi nomzod (bitta test — bitta test dasturi).
        foreach (var untouched in new[] { SystemProgramId, EmptyArchivedProgramId, ArchivedSoloProgramId, LaterFormsCopyProgramId })
        {
            (await SqlHelpers.CountAsync(connectionString, $"assessment_programs WHERE id = '{untouched}' AND owner_test_definition_id IS NULL", ct))
                .Should().Be(1, $"{untouched} dasturi test dasturiga aylanmasligi kerak");
        }

        (await SqlHelpers.ScalarAsync<string>(connectionString, $"SELECT code FROM assessment_programs WHERE id = '{EmptyArchivedProgramId}'", ct))
            .Should().Be("1");

        // (e) Faol dastur, lekin testi hali Draft → test dasturi bo'ladi, lekin to'xtatiladi.
        (await SqlHelpers.ScalarAsync<Guid>(connectionString, $"SELECT owner_test_definition_id FROM assessment_programs WHERE id = '{DraftTestProgramId}'", ct))
            .Should().Be(DraftTestId);
        (await SqlHelpers.ScalarAsync<bool>(connectionString, $"SELECT is_active FROM assessment_programs WHERE id = '{DraftTestProgramId}'", ct))
            .Should().BeFalse("test o'quvchiga ochiq emas — landing'da bo'sh dastur chiqmasin");

        (await SqlHelpers.CountAsync(connectionString, "assessment_programs WHERE owner_test_definition_id IS NOT NULL", ct)).Should().Be(2);

        // (f) IDEMPOTENT: SQL qayta bajarilsa hech narsa o'zgarmaydi.
        await SqlHelpers.ExecuteAsync(connectionString, AddTestProgramOwner.ConvertSingleTestProgramsSql, ct);
        (await SqlHelpers.CountAsync(connectionString, "assessment_programs WHERE owner_test_definition_id IS NOT NULL", ct)).Should().Be(2);
        (await SqlHelpers.CountAsync(connectionString, $"assessment_programs WHERE id = '{LaterFormsCopyProgramId}' AND owner_test_definition_id IS NULL", ct)).Should().Be(1);

        // (g) Bitta test — ko'pi bilan bitta test dasturi (qisman unikal indeks).
        var duplicate = async () => await SqlHelpers.ExecuteAsync(
            connectionString,
            $"UPDATE assessment_programs SET owner_test_definition_id = '{IntellectTestId}' WHERE id = '{LaterFormsCopyProgramId}'",
            ct);
        await duplicate.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23505");
    }

    /// <summary>`AddStudentProfileExtra` holatidagi sxemaga mos (faqat o'sha paytdagi ustunlar) jonli bazaga o'xshash ma'lumot.</summary>
    private static async Task SeedLiveLikeDataAsync(string connectionString, CancellationToken ct)
    {
        var assessmentRows = string.Join(
            ",\n",
            Enumerable.Range(1, 4).Select(i => $"""
                (gen_random_uuid(), '{StudentId}', '{(i % 2 == 0 ? SchoolAId : SchoolBId)}', 'forms-session-{i:D4}',
                 'forms-session-hash-{i:D4}', now() + interval '7 days', '{FormsProgramId}')
                """));

        await SqlHelpers.ExecuteAsync(connectionString, $"""
            INSERT INTO schools (id, name, region, district, slug, access_token, kind)
            VALUES ('{SchoolAId}', 'A maktab', 'Toshkent', 'Yunusobod', 'a-maktab', 'forms-access-token-a-000000000001', 1),
                   ('{SchoolBId}', 'B maktab', 'Toshkent', 'Chilonzor', 'b-maktab', 'forms-access-token-b-000000000002', 1);

            INSERT INTO students (id, school_id, full_name, normalized_name, grade, consent_given_at)
            VALUES ('{StudentId}', '{SchoolAId}', 'Ali Valiyev', 'ali valiyev', 9, now());

            INSERT INTO test_definitions (id, code, name_uz, description_uz, display_order, estimated_minutes, status, is_active, kind, scoring_mode)
            VALUES ('{IntellectTestId}', 'INTELLECT-SURVEY', 'Intellekt so''rovnomasi', 'Test tavsifi', 7, 20, 2, true, 2, 2),
                   ('{MbtiTestId}', 'MBTI16', 'Shaxsiyat tipi', NULL, 1, 15, 2, true, 1, 1),
                   ('{Big5TestId}', 'BIG5', 'Big Five', NULL, 2, 12, 2, true, 1, 1),
                   ('{ArchivedSoloTestId}', 'SOLO', 'Yakka test', NULL, 3, 5, 2, true, 2, 2),
                   ('{DraftTestId}', 'DRAFT-T', 'Qoralama test', NULL, 4, 5, 1, true, 2, 2);

            -- Tizim dasturi (`RequireAssessmentProgramId` yaratgan) — ko'p testli va arxivlangan.
            UPDATE assessment_programs SET status = 3, is_active = false WHERE id = '{SystemProgramId}';
            INSERT INTO program_tests (id, program_id, test_definition_id, display_order)
            VALUES (gen_random_uuid(), '{SystemProgramId}', '{MbtiTestId}', 1),
                   (gen_random_uuid(), '{SystemProgramId}', '{Big5TestId}', 2);

            INSERT INTO assessment_programs (id, code, name_uz, display_order, kind, visibility, status, is_active, is_system, created_at)
            VALUES ('{FormsProgramId}', 'FORMS', 'Anketalar', 1, 2, 2, 2, true, false, now() - interval '10 days'),
                   ('{LaterFormsCopyProgramId}', 'FORMS-2', 'Anketalar nusxa', 2, 2, 2, 2, true, false, now() - interval '1 day'),
                   ('{EmptyArchivedProgramId}', '1', '1', 3, 2, 2, 3, false, false, now()),
                   ('{ArchivedSoloProgramId}', 'OLD', 'Eski', 4, 2, 2, 3, false, false, now()),
                   ('{DraftTestProgramId}', 'DR', 'Qoralama dastur', 5, 2, 2, 2, true, false, now());

            INSERT INTO program_tests (id, program_id, test_definition_id, display_order)
            VALUES (gen_random_uuid(), '{FormsProgramId}', '{IntellectTestId}', 1),
                   (gen_random_uuid(), '{LaterFormsCopyProgramId}', '{IntellectTestId}', 1),
                   (gen_random_uuid(), '{ArchivedSoloProgramId}', '{ArchivedSoloTestId}', 1),
                   (gen_random_uuid(), '{DraftTestProgramId}', '{DraftTestId}', 1);

            INSERT INTO school_programs (id, school_id, program_id)
            VALUES (gen_random_uuid(), '{SchoolAId}', '{FormsProgramId}'),
                   (gen_random_uuid(), '{SchoolBId}', '{FormsProgramId}');

            INSERT INTO assessments (id, student_id, school_id, session_token, session_token_hash, expires_at, program_id)
            VALUES
            {assessmentRows};
            """, ct);
    }
}

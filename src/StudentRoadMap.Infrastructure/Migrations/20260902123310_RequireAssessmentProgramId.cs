using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RequireAssessmentProgramId : Migration
    {
        /// <summary>
        /// `PERSONALITY_PROFILE` tizim dasturining deterministik ID'si — `DbSeeder.
        /// SystemPersonalityProfileProgramId` bilan AYNAN bir xil bo'lishi SHART (QA
        /// tuzatmasi, 2026-09-02: ikkalasi mustaqil `Guid.NewGuid()` bilan yaratsa, ular
        /// ikki xil dastur hosil qilib qo'yardi — `--migrate` butun zanjirni `--seed`dan
        /// OLDIN bajaradi, `docker-compose.yml`: migrate → seed → api).
        /// </summary>
        private const string SystemProgramId = "00000000-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ Ikki bosqichli migratsiya (`CLAUDE.md` 7-qoida) — YAKUNIY bosqich. Bloklovchi
            // tuzatma (QA, 2026-09-02): `program_id`ni `NOT NULL` qilishdan OLDIN mavjud
            // `NULL` qatorlar HAQIQIY tizim dasturiga bog'lanishi shart — `DbSeeder.
            // SeedSystemProgramAsync`ning backfill'iga TAYANIB BO'LMAYDI, chunki u faqat
            // `--seed` bosqichida (bu migratsiyadan KEYIN, alohida jarayon chaqiruvida)
            // ishga tushadi. Shu sabab backfill shu YERDA, xom SQL bilan, `SET NOT NULL`dan
            // OLDIN bajariladi:
            //   1) `PERSONALITY_PROFILE` dasturi (deterministik ID bilan) mavjud bo'lmasa yaratiladi;
            //   2) mavjud 4 tizim metodikasi (agar test banki bu paytda allaqachon seed
            //      qilingan bo'lsa — eski/upgrade holatida shunday) `program_tests`ga bog'lanadi;
            //   3) `program_id IS NULL` bo'lgan barcha `assessments` shu dasturga bog'lanadi;
            //   4) shundan KEYIN ustun `NOT NULL` qilinadi.
            // Idempotent (`WHERE NOT EXISTS`/`WHERE ... IS NULL`) — qayta ishga tushirilsa ham xavfsiz.
            // Yangi (bo'sh) o'rnatishda `assessments`/`test_definitions` bo'sh bo'lgani uchun
            // 1-2-3-qadamlar 0 qatorga tegadi — `PERSONALITY_PROFILE` baribir yaratiladi (bo'sh
            // tarkib bilan), `--seed` keyinroq `DbSeeder.EnsureSystemTestsAttached` orqali
            // haqiqiy 4 testni bog'laydi.
            migrationBuilder.Sql($"""
                INSERT INTO assessment_programs
                    (id, code, name_uz, description_uz, kind, visibility, status, is_active, display_order, is_system, created_by_admin_user_id, created_at, updated_at)
                SELECT
                    '{SystemProgramId}'::uuid,
                    'PERSONALITY_PROFILE',
                    'Shaxsiyat profili',
                    'To''rt ilmiy metodikadan iborat yaxlit batareya: shaxsiyat tipi, Big Five, kasb qiziqishlari va aktivlik.',
                    1, -- ProgramKind.System
                    1, -- ProgramVisibility.Public
                    2, -- ProgramStatus.Published
                    true,
                    1,
                    true,
                    NULL,
                    now(),
                    now()
                WHERE NOT EXISTS (SELECT 1 FROM assessment_programs WHERE code = 'PERSONALITY_PROFILE');
                """);

            migrationBuilder.Sql($"""
                INSERT INTO program_tests (id, program_id, test_definition_id, display_order)
                SELECT gen_random_uuid(), '{SystemProgramId}'::uuid, td.id, td.display_order
                FROM test_definitions td
                WHERE td.code IN ('MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY')
                  AND NOT EXISTS (
                      SELECT 1 FROM program_tests pt
                      WHERE pt.program_id = '{SystemProgramId}'::uuid
                        AND pt.test_definition_id = td.id
                  );
                """);

            migrationBuilder.Sql($"""
                UPDATE assessments SET program_id = '{SystemProgramId}'::uuid WHERE program_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "program_id",
                table: "assessments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ma'lumot (seed qilingan `PERSONALITY_PROFILE` dasturi/bog'lanishlar) ATAYLAB
            // orqaga qaytarilmaydi — faqat ustun cheklovi bekor qilinadi (boshqa migratsiyalar
            // Down()idagi kabi, ma'lumot yo'qotishsiz qaytariladigan yagona qism).
            migrationBuilder.AlterColumn<Guid>(
                name: "program_id",
                table: "assessments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}

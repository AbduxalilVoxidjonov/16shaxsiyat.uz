using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <summary>
    /// **2026-09-23 egasi qarori** (`docs/18` §9.7, `docs/05`): admin UI'dan "Dasturlar" bo'limi
    /// olib tashlandi, biriktirish test ichida qilinadi. `assessment_programs` ICHKI qatlam
    /// bo'lib qoladi; har test uchun ko'pi bilan bitta "test dasturi" —
    /// `owner_test_definition_id` (qisman unikal indeks, FK `RESTRICT`).
    ///
    /// <para>
    /// **Ma'lumot migratsiyasi (idempotent, yangi dastur YARATILMAYDI):** mavjud Custom,
    /// tizimga tegishli bo'lmagan, nashr qilingan va FAOL, AYNAN bitta testli dastur — agar
    /// o'sha test uchun hali test dasturi bo'lmasa — o'sha testning test dasturi deb
    /// belgilanadi (jonli bazada `FORMS` → `INTELLECT-SURVEY`). Shu tufayli uning
    /// `school_programs` biriktirmalari va sessiyalari (`assessments.program_id`) saqlanadi.
    /// Nomi/tavsifi/tartibi testdan ko'chiriladi, kodi — test kodi (band bo'lmasa). Test o'zi
    /// ochiq bo'lmasa (`Published` + faol emas) dastur to'xtatiladi (`is_active = false`).
    /// Ko'p testli yoki arxivlangan/nofaol dasturlar (`PERSONALITY_PROFILE`, `1`) TEGILMAYDI.
    /// Bitta testga bir nechta nomzod bo'lsa — eng avval yaratilgani.
    /// </para>
    /// </summary>
    public partial class AddTestProgramOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_test_definition_id",
                table: "assessment_programs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_assessment_programs_owner_test",
                table: "assessment_programs",
                column: "owner_test_definition_id",
                unique: true,
                filter: "owner_test_definition_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_assessment_programs_test_definitions_owner_test_definition_",
                table: "assessment_programs",
                column: "owner_test_definition_id",
                principalTable: "test_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Qayta ishga tushirilsa ham xavfsiz: nomzod faqat `owner_test_definition_id IS NULL`
            // va shu test uchun hali egasi yo'q bo'lsa tanlanadi.
            migrationBuilder.Sql(ConvertSingleTestProgramsSql);
        }

        public const string ConvertSingleTestProgramsSql = """
            WITH candidates AS (
                SELECT DISTINCT ON (pt.test_definition_id)
                       p.id AS program_id,
                       pt.test_definition_id
                FROM assessment_programs p
                JOIN program_tests pt ON pt.program_id = p.id
                WHERE p.owner_test_definition_id IS NULL
                  AND p.kind = 2
                  AND p.is_system = false
                  AND p.status = 2
                  AND p.is_active = true
                  AND (SELECT count(*) FROM program_tests x WHERE x.program_id = p.id) = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM assessment_programs o
                      WHERE o.owner_test_definition_id = pt.test_definition_id)
                ORDER BY pt.test_definition_id, p.created_at, p.id
            )
            UPDATE assessment_programs p
            SET owner_test_definition_id = c.test_definition_id,
                name_uz = t.name_uz,
                description_uz = t.description_uz,
                display_order = t.display_order,
                code = CASE
                    WHEN NOT EXISTS (
                        SELECT 1 FROM assessment_programs o
                        WHERE o.code = t.code AND o.id <> p.id)
                    THEN t.code
                    ELSE p.code
                END,
                is_active = CASE WHEN t.status = 2 AND t.is_active THEN p.is_active ELSE false END,
                updated_at = now()
            FROM candidates c
            JOIN test_definitions t ON t.id = c.test_definition_id
            WHERE p.id = c.program_id;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ma'lumot qismi ortga qaytarilmaydi: nom/kod testdan olingan bo'lib qoladi (zararsiz),
            // `owner_test_definition_id` esa ustun bilan birga yo'qoladi.
            migrationBuilder.DropForeignKey(
                name: "fk_assessment_programs_test_definitions_owner_test_definition_",
                table: "assessment_programs");

            migrationBuilder.DropIndex(
                name: "ux_assessment_programs_owner_test",
                table: "assessment_programs");

            migrationBuilder.DropColumn(
                name: "owner_test_definition_id",
                table: "assessment_programs");
        }
    }
}

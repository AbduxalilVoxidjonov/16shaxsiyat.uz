using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <summary>
    /// Maktab kodi (`schools.entry_code`, `docs/08` 3a-bo'lim) — `/kirish` → "Maktab uchun" yo'li.
    ///
    /// **Ikki bosqichli, buzilmaydigan migratsiya (`CLAUDE.md` 7-qoida):**
    ///   1. ustun NULLABLE qo'shiladi;
    ///   2. MAVJUD maktablar (`kind = 1`, o'chirilganlar ham — kod qayta berilmasligi uchun)
    ///      bazaning O'ZIDA unikal kod bilan to'ldiriladi (pastga qarang); ommaviy makon
    ///      (`kind = 2`) `NULL` qoladi — unga kod bilan kirish yo'q (faqat Telegram);
    ///   3. qisman unikal indeks `ux_schools_entry_code` (`entry_code IS NOT NULL`).
    /// `SET NOT NULL` ATAYLAB QILINMAYDI — ommaviy makon sababli ustun doim nullable;
    /// "`kind = 1` uchun har doim to'ldirilgan" invariantini domen (`School.Create`) kafolatlaydi.
    ///
    /// **Backfill** `Domain.Schools.SchoolEntryCode` bilan BIR XIL format: 8 belgi, alifbo
    /// `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (31 belgi, `0 O 1 I L` yo'q). Tasodifiylik —
    /// pgcrypto `gen_random_bytes` (kengaytma `InitialCreate` da yoqilgan), har bayt uchun
    /// REJECTION SAMPLING (`< 248 = 31·8` bo'lgan baytlar olinadi, keyin `% 31`) — modul
    /// og'ishi yo'q, ya'ni backfill kodlari ishlab chiqarish generatori
    /// (`EntryCodeGenerator`, `RandomNumberGenerator.GetInt32`) bilan bir xil taqsimotda.
    /// To'qnashish (31^8 fazoda amalda yo'q) `EXIT WHEN NOT EXISTS` sikli bilan yopiladi.
    /// Admin xohlasa istalgan kodni `regenerate-entry-code` bilan yangilaydi.
    ///
    /// Bo'sh bazada (yangi o'rnatish) `DO` bloki hech narsa qilmaydi — `MigrateFromScratchTests`
    /// sxema tengligi saqlanadi (`DEFAULT` yo'q, ustun modeldagi bilan bir xil).
    /// </summary>
    public partial class AddSchoolEntryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "entry_code",
                table: "schools",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            // 2-bosqich: mavjud maktablar uchun unikal kod (sinf izohiga qarang). Ommaviy makon
            // (`kind = 2`) ATAYLAB tashlab ketiladi — `entry_code` unda `NULL`.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    alphabet CONSTANT text := 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
                    school_row RECORD;
                    candidate text;
                    byte_value int;
                BEGIN
                    FOR school_row IN SELECT id FROM schools WHERE kind = 1 AND entry_code IS NULL LOOP
                        LOOP
                            candidate := '';
                            WHILE length(candidate) < 8 LOOP
                                byte_value := get_byte(gen_random_bytes(1), 0);
                                -- Rejection sampling: 248 = 31 * 8 — faqat shu chegaradan pastdagi
                                -- baytlar olinadi, shunda `% 31` tekis taqsimot beradi.
                                IF byte_value < 248 THEN
                                    candidate := candidate || substr(alphabet, (byte_value % 31) + 1, 1);
                                END IF;
                            END LOOP;
                            EXIT WHEN NOT EXISTS (SELECT 1 FROM schools WHERE entry_code = candidate);
                        END LOOP;
                        UPDATE schools SET entry_code = candidate WHERE id = school_row.id;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_schools_entry_code",
                table: "schools",
                column: "entry_code",
                unique: true,
                filter: "entry_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_schools_entry_code",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "entry_code",
                table: "schools");
        }
    }
}

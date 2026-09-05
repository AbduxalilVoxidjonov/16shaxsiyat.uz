using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <summary>
    /// P47 — ommaviy (maktabsiz) oqim uchun birinchi qatlam: makon turi, ommaviy foydalanuvchi
    /// akkaunti va sessiya tokenini xeshlash.
    ///
    /// **Buzilmaydigan (non-breaking) migratsiya.** Uchta joyda ustun `NOT NULL` bo'lishi kerak,
    /// lekin EF ning odatiy `AddColumn(nullable: false, defaultValue: ...)` shakli Postgres'da
    /// ustunga DOIMIY `DEFAULT` qoldiradi va u modelda yo'q — natijada
    /// `MigratsiyaSxemasi_ModeldanQurilganSxemaBilanBirXil` testi qizil bo'lardi (loyihada
    /// bunday texnik qarz allaqachon bo'lgan: `admin_users.concurrency_stamp`, keyin
    /// `DropConcurrencyStampDefault` bilan tozalangan). Shu sabab bu yerda uch qadamli naqsh
    /// ishlatiladi: `nullable qo'sh → backfill → SET NOT NULL`.
    ///
    /// **`assessments.session_token_hash` va mavjud faol sessiyalar.** Sessiya tokeni ilgari
    /// OCHIQ matnda saqlanardi (refresh tokenlardan farqli — `docs/08` 2-bo'lim). Endi qo'shilgan
    /// xesh ustuni MAVJUD qatorlar uchun bazaning O'ZIDA hisoblanadi
    /// (`encode(sha256(convert_to(session_token,'UTF8')),'hex')` — `Domain.Common.TokenHash.Compute`
    /// bilan bayt-ma-bayt bir xil natija). Ya'ni HECH BIR faol sessiya bekor qilinmaydi:
    /// o'quvchi test yechayotgan brauzerdagi token ishlashda davom etadi. Muqobil variant
    /// (barcha tokenlarni bekor qilish) ataylab TANLANMADI — o'rtasida test yechayotgan
    /// o'quvchilarning javoblari yo'qolardi.
    ///
    /// Ochiq matnli `session_token` ustuni bu migratsiyada O'CHIRILMAYDI — ikki bosqichli
    /// destruktiv o'zgarish qoidasi (`CLAUDE.md` 7-qoida): avval o'qish yo'li xeshga
    /// ko'chiriladi (`SessionTokenAuthenticationHandler` + `StartSession` rezyume yo'li),
    /// keyin alohida migratsiya ustunni tashlaydi.
    /// </summary>
    public partial class AddPublicSpaceAndPublicUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_students_identity",
                table: "students");

            migrationBuilder.DropCheckConstraint(
                name: "ck_students_grade",
                table: "students");

            migrationBuilder.AddColumn<string>(
                name: "consent_version",
                table: "students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "parental_consent",
                table: "students",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "public_user_id",
                table: "students",
                type: "uuid",
                nullable: true);

            // Makon turi: 1 = School, 2 = PublicSpace (`docs/05` 3-bo'lim). Mavjud barcha
            // yozuv — haqiqiy maktab, shu sabab backfill qiymati 1.
            migrationBuilder.AddColumn<short>(
                name: "kind",
                table: "schools",
                type: "smallint",
                nullable: true);

            migrationBuilder.Sql("UPDATE schools SET kind = 1 WHERE kind IS NULL;");
            migrationBuilder.Sql("ALTER TABLE schools ALTER COLUMN kind SET NOT NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "show_result_to_student",
                table: "schools",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Sessiya tokenining SHA-256 xeshi. Backfill bazada bajariladi — sinf izohiga
            // qarang: mavjud faol sessiyalar buzilmaydi.
            migrationBuilder.AddColumn<string>(
                name: "session_token_hash",
                table: "assessments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE assessments SET session_token_hash = encode(sha256(convert_to(session_token, 'UTF8')), 'hex') WHERE session_token_hash IS NULL;");
            migrationBuilder.Sql("ALTER TABLE assessments ALTER COLUMN session_token_hash SET NOT NULL;");

            migrationBuilder.CreateTable(
                name: "public_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    telegram_id = table.Column<long>(type: "bigint", nullable: true),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_public_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "public_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    public_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_public_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_public_refresh_tokens_public_users_public_user_id",
                        column: x => x.public_user_id,
                        principalTable: "public_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_students_identity",
                table: "students",
                columns: new[] { "school_id", "normalized_name", "birth_date" },
                unique: true,
                filter: "is_deleted = false AND public_user_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_students_public_user",
                table: "students",
                column: "public_user_id",
                unique: true,
                filter: "public_user_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_students_grade",
                table: "students",
                sql: "grade BETWEEN 0 AND 11");

            migrationBuilder.CreateIndex(
                name: "ux_schools_public_space",
                table: "schools",
                column: "kind",
                unique: true,
                filter: "kind = 2 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_assessments_token_hash",
                table: "assessments",
                column: "session_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_public_refresh_tokens_user",
                table: "public_refresh_tokens",
                column: "public_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_public_refresh_tokens_hash",
                table: "public_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_public_users_created",
                table: "public_users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_public_users_telegram",
                table: "public_users",
                column: "telegram_id",
                unique: true,
                filter: "telegram_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_students_public_users_public_user_id",
                table: "students",
                column: "public_user_id",
                principalTable: "public_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_students_public_users_public_user_id",
                table: "students");

            migrationBuilder.DropTable(
                name: "public_refresh_tokens");

            migrationBuilder.DropTable(
                name: "public_users");

            migrationBuilder.DropIndex(
                name: "ux_students_identity",
                table: "students");

            migrationBuilder.DropIndex(
                name: "ux_students_public_user",
                table: "students");

            migrationBuilder.DropCheckConstraint(
                name: "ck_students_grade",
                table: "students");

            migrationBuilder.DropIndex(
                name: "ux_schools_public_space",
                table: "schools");

            migrationBuilder.DropIndex(
                name: "ux_assessments_token_hash",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "consent_version",
                table: "students");

            migrationBuilder.DropColumn(
                name: "parental_consent",
                table: "students");

            migrationBuilder.DropColumn(
                name: "public_user_id",
                table: "students");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "show_result_to_student",
                table: "schools");

            migrationBuilder.DropColumn(
                name: "session_token_hash",
                table: "assessments");

            migrationBuilder.CreateIndex(
                name: "ux_students_identity",
                table: "students",
                columns: new[] { "school_id", "normalized_name", "birth_date" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_students_grade",
                table: "students",
                sql: "grade BETWEEN 1 AND 11");
        }
    }
}

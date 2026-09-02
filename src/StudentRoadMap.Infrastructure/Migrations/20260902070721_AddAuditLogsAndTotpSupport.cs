using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogsAndTotpSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "admin_users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "totp_last_used_step",
                table: "admin_users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_totp_backup_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_totp_backup_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_totp_backup_codes_admin_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_admin_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_totp_backup_codes_admin",
                table: "admin_totp_backup_codes",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_admin_user_id",
                table: "audit_logs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created",
                table: "audit_logs",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_totp_backup_codes");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "totp_last_used_step",
                table: "admin_users");
        }
    }
}

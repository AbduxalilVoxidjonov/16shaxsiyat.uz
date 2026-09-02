using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisJobsAndFallbackReportFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_fallback_report",
                table: "ai_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "analysis_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_provider = table.Column<short>(type: "smallint", nullable: true),
                    requested_prompt_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_jobs_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_scales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    test_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name_uz = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description_uz = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    interpretation_bands_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_scales", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_scales_test_definitions_test_definition_id",
                        column: x => x.test_definition_id,
                        principalTable: "test_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_status",
                table: "analysis_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_analysis_jobs_active_per_assessment",
                table: "analysis_jobs",
                column: "assessment_id",
                unique: true,
                filter: "status IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "ux_test_scales",
                table: "test_scales",
                columns: new[] { "test_definition_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_jobs");

            migrationBuilder.DropTable(
                name: "test_scales");

            migrationBuilder.DropColumn(
                name: "is_fallback_report",
                table: "ai_analyses");
        }
    }
}

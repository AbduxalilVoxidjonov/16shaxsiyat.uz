using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "scoring_strategy",
                table: "test_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<short>(
                name: "scoring_mode",
                table: "test_definitions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<Guid>(
                name: "program_id",
                table: "assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assessment_programs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name_uz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description_uz = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    visibility = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by_admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_programs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "program_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_tests_assessment_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "assessment_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_program_tests_test_definitions_test_definition_id",
                        column: x => x.test_definition_id,
                        principalTable: "test_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "school_programs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_programs", x => x.id);
                    table.ForeignKey(
                        name: "fk_school_programs_assessment_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "assessment_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_school_programs_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_program",
                table: "assessments",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessment_programs_active",
                table: "assessment_programs",
                columns: new[] { "is_active", "display_order" },
                filter: "status = 2");

            migrationBuilder.CreateIndex(
                name: "ux_assessment_programs_code",
                table: "assessment_programs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_program_tests_order",
                table: "program_tests",
                columns: new[] { "program_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_program_tests_test_definition_id",
                table: "program_tests",
                column: "test_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_program_tests",
                table: "program_tests",
                columns: new[] { "program_id", "test_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_school_programs_program",
                table: "school_programs",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "ux_school_programs",
                table: "school_programs",
                columns: new[] { "school_id", "program_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_assessments_assessment_programs_program_id",
                table: "assessments",
                column: "program_id",
                principalTable: "assessment_programs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assessments_assessment_programs_program_id",
                table: "assessments");

            migrationBuilder.DropTable(
                name: "program_tests");

            migrationBuilder.DropTable(
                name: "school_programs");

            migrationBuilder.DropTable(
                name: "assessment_programs");

            migrationBuilder.DropIndex(
                name: "ix_assessments_program",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "scoring_mode",
                table: "test_definitions");

            migrationBuilder.DropColumn(
                name: "program_id",
                table: "assessments");

            migrationBuilder.AlterColumn<string>(
                name: "scoring_strategy",
                table: "test_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}

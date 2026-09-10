using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchingSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "input_pattern",
                table: "questions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_length",
                table: "questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_selections",
                table: "questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_selections",
                table: "questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "placeholder",
                table: "questions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility_rule",
                table: "questions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "raw_value",
                table: "answers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "selected_values",
                table: "answers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_value",
                table: "answers",
                type: "text",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "question_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    test_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title_uz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_uz = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    visibility_rule = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_question_sections_test_definitions_test_definition_id",
                        column: x => x.test_definition_id,
                        principalTable: "test_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_questions_section",
                table: "questions",
                columns: new[] { "section_id", "display_order" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_answers_shape",
                table: "answers",
                sql: "(raw_value IS NOT NULL)::int\n+ (text_value IS NOT NULL)::int\n+ (selected_values IS NOT NULL)::int = 1");

            migrationBuilder.CreateIndex(
                name: "ix_question_sections_test_order",
                table: "question_sections",
                columns: new[] { "test_definition_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_question_sections_test_code",
                table: "question_sections",
                columns: new[] { "test_definition_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_questions_question_sections_section_id",
                table: "questions",
                column: "section_id",
                principalTable: "question_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_questions_question_sections_section_id",
                table: "questions");

            migrationBuilder.DropTable(
                name: "question_sections");

            migrationBuilder.DropIndex(
                name: "ix_questions_section",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_answers_shape",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "input_pattern",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "max_length",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "max_selections",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "min_selections",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "placeholder",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "visibility_rule",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "selected_values",
                table: "answers");

            migrationBuilder.DropColumn(
                name: "text_value",
                table: "answers");

            migrationBuilder.AlterColumn<int>(
                name: "raw_value",
                table: "answers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

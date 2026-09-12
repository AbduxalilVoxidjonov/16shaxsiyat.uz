using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProfileExtra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profile_extra",
                table: "students",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_extra",
                table: "students");
        }
    }
}

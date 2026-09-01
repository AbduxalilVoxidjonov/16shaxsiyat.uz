using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_counters",
                columns: table => new
                {
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_counters", x => new { x.school_id, x.date_utc });
                    table.ForeignKey(
                        name: "fk_registration_counters_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_counters");
        }
    }
}

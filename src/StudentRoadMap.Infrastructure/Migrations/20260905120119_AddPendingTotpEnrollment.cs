using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingTotpEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_totp_created_at",
                table: "admin_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_totp_secret_encrypted",
                table: "admin_users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_totp_created_at",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "pending_totp_secret_encrypted",
                table: "admin_users");
        }
    }
}

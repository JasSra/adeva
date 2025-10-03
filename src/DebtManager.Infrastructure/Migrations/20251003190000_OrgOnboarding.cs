using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManager.Infrastructure.Migrations
{
    public partial class OrgOnboarding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abn",
                table: "Organizations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Organizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations",
                column: "Abn");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Abn",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "Organizations");
        }
    }
}

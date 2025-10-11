using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgApplicationAndBankFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationApprovalNote",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationRejectionReason",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountBsb",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationNotes",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerifiedAtUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerifiedBy",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationApprovalNote",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ApplicationRejectionReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BankAccountBsb",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationNotes",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAtUtc",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedBy",
                table: "Organizations");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DebtManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Weeks",
                table: "PaymentPlans");

            migrationBuilder.RenameColumn(
                name: "OccurredAtUtc",
                table: "Transactions",
                newName: "ProcessedAtUtc");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "PaymentPlans",
                newName: "TotalPayable");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Debts",
                newName: "PortfolioCode");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Debts",
                newName: "OutstandingPrincipal");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderRef",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentPlanId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DebtId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DebtorId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentInstallmentId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAtUtc",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "PaymentPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "PaymentPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "PaymentPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "PaymentPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DefaultedAtUtc",
                table: "PaymentPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PaymentPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DownPaymentAmount",
                table: "PaymentPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DownPaymentDueAtUtc",
                table: "PaymentPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateUtc",
                table: "PaymentPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "PaymentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodInDays",
                table: "PaymentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallmentAmount",
                table: "PaymentPlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "PaymentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PaymentPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "PaymentPlans",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualReview",
                table: "PaymentPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateUtc",
                table: "PaymentPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PaymentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColorHex",
                table: "Organizations",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Abn",
                table: "Organizations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactEmail",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactPhone",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandTagline",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBrandRefreshAtUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReconciliationAtUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardedAtUtc",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColorHex",
                table: "Organizations",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatementFooter",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupportPhone",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TradingName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AccruedFees",
                table: "Debts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AccruedInterest",
                table: "Debts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AssignedCollectorUserId",
                table: "Debts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Debts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientReferenceNumber",
                table: "Debts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "Debts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "Debts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDateUtc",
                table: "Debts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                table: "Debts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GraceDays",
                table: "Debts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InterestCalculationMethod",
                table: "Debts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRateAnnualPercentage",
                table: "Debts",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentAtUtc",
                table: "Debts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeFlat",
                table: "Debts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeePercentage",
                table: "Debts",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextActionAtUtc",
                table: "Debts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Debts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAtUtc",
                table: "Debts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Debts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrincipal",
                table: "Debts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SettlementOfferAmount",
                table: "Debts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettlementOfferExpiresAtUtc",
                table: "Debts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Debts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WriteOffReason",
                table: "Debts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Debtors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AlternatePhone",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Debtors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerName",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GovernmentId",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncomeBracket",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactedAtUtc",
                table: "Debtors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "Debtors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PortalAccessEnabled",
                table: "Debtors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreferredContactMethod",
                table: "Debtors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredName",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Debtors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TagsCsv",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PaymentInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentInstallments_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DebtId_ProcessedAtUtc",
                table: "Transactions",
                columns: new[] { "DebtId", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_DebtorId",
                table: "Transactions",
                column: "DebtorId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentInstallmentId",
                table: "Transactions",
                column: "PaymentInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentPlanId",
                table: "Transactions",
                column: "PaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProviderRef",
                table: "Transactions",
                column: "ProviderRef");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_DebtId",
                table: "PaymentPlans",
                column: "DebtId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_Reference",
                table: "PaymentPlans",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations",
                column: "Abn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Debts_DebtorId",
                table: "Debts",
                column: "DebtorId");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_ExternalAccountId",
                table: "Debts",
                column: "ExternalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_OrganizationId_ClientReferenceNumber",
                table: "Debts",
                columns: new[] { "OrganizationId", "ClientReferenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Debtors_OrganizationId_Email",
                table: "Debtors",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstallments_PaymentPlanId_Sequence",
                table: "PaymentInstallments",
                columns: new[] { "PaymentPlanId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Debtors_Organizations_OrganizationId",
                table: "Debtors",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Debts_Debtors_DebtorId",
                table: "Debts",
                column: "DebtorId",
                principalTable: "Debtors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Debts_Organizations_OrganizationId",
                table: "Debts",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentPlans_Debts_DebtId",
                table: "PaymentPlans",
                column: "DebtId",
                principalTable: "Debts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Debtors_DebtorId",
                table: "Transactions",
                column: "DebtorId",
                principalTable: "Debtors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Debts_DebtId",
                table: "Transactions",
                column: "DebtId",
                principalTable: "Debts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentInstallments_PaymentInstallmentId",
                table: "Transactions",
                column: "PaymentInstallmentId",
                principalTable: "PaymentInstallments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_PaymentPlans_PaymentPlanId",
                table: "Transactions",
                column: "PaymentPlanId",
                principalTable: "PaymentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debtors_Organizations_OrganizationId",
                table: "Debtors");

            migrationBuilder.DropForeignKey(
                name: "FK_Debts_Debtors_DebtorId",
                table: "Debts");

            migrationBuilder.DropForeignKey(
                name: "FK_Debts_Organizations_OrganizationId",
                table: "Debts");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentPlans_Debts_DebtId",
                table: "PaymentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Debtors_DebtorId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Debts_DebtId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentInstallments_PaymentInstallmentId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_PaymentPlans_PaymentPlanId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "PaymentInstallments");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_DebtId_ProcessedAtUtc",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_DebtorId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentInstallmentId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentPlanId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ProviderRef",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_DebtId",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_Reference",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Debts_DebtorId",
                table: "Debts");

            migrationBuilder.DropIndex(
                name: "IX_Debts_ExternalAccountId",
                table: "Debts");

            migrationBuilder.DropIndex(
                name: "IX_Debts_OrganizationId_ClientReferenceNumber",
                table: "Debts");

            migrationBuilder.DropIndex(
                name: "IX_Debtors_OrganizationId_Email",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DebtId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DebtorId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FeeCurrency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentInstallmentId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SettledAtUtc",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "DefaultedAtUtc",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "DownPaymentAmount",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "DownPaymentDueAtUtc",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "EndDateUtc",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "GracePeriodInDays",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "InstallmentAmount",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "RequiresManualReview",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "StartDateUtc",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "BillingContactEmail",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BillingContactName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BillingContactPhone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BrandTagline",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastBrandRefreshAtUtc",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "NextReconciliationAtUtc",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OnboardedAtUtc",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SecondaryColorHex",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StatementFooter",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SupportPhone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "TradingName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "AccruedFees",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "AccruedInterest",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "AssignedCollectorUserId",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "ClientReferenceNumber",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "DueDateUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "GraceDays",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "InterestCalculationMethod",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "InterestRateAnnualPercentage",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "LastPaymentAtUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "LateFeeFlat",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "LateFeePercentage",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "NextActionAtUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "OpenedAtUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "OriginalPrincipal",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "SettlementOfferAmount",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "SettlementOfferExpiresAtUtc",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "WriteOffReason",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "AlternatePhone",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "EmployerName",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "GovernmentId",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "IncomeBracket",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "LastContactedAtUtc",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "PortalAccessEnabled",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "PreferredContactMethod",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "PreferredName",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "TagsCsv",
                table: "Debtors");

            migrationBuilder.RenameColumn(
                name: "ProcessedAtUtc",
                table: "Transactions",
                newName: "OccurredAtUtc");

            migrationBuilder.RenameColumn(
                name: "TotalPayable",
                table: "PaymentPlans",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "PortfolioCode",
                table: "Debts",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "OutstandingPrincipal",
                table: "Debts",
                newName: "Amount");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderRef",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentPlanId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Weeks",
                table: "PaymentPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColorHex",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Abn",
                table: "Organizations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Debtors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Abn",
                table: "Organizations",
                column: "Abn");
        }
    }
}

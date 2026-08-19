using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBankingFixedDepositsSchema : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_accounts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    branch_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    chart_of_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fixed_deposit_interest_accruals",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixed_deposit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    accounting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversal_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixed_deposit_interest_accruals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fixed_deposit_interest_receipts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixed_deposit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deduction_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    receiving_financial_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversal_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixed_deposit_interest_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fixed_deposits",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    funding_financial_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_financial_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    interest_rate_percent = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    maturity_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_gross_interest = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expected_deduction_rate_percent = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    predecessor_fixed_deposit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    successor_fixed_deposit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    placement_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    renewal_adjustment_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    withdrawal_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    void_reversal_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixed_deposits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_accounts_tenant_id_is_active",
                schema: "finance",
                table: "financial_accounts",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_fd_interest_accruals_tenant_id_fixed_deposit_id",
                schema: "finance",
                table: "fixed_deposit_interest_accruals",
                columns: new[] { "tenant_id", "fixed_deposit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fd_interest_receipts_tenant_id_fixed_deposit_id",
                schema: "finance",
                table: "fixed_deposit_interest_receipts",
                columns: new[] { "tenant_id", "fixed_deposit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fixed_deposits_tenant_id_fund_id",
                schema: "finance",
                table: "fixed_deposits",
                columns: new[] { "tenant_id", "fund_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fixed_deposits_tenant_id_funding_financial_account_id",
                schema: "finance",
                table: "fixed_deposits",
                columns: new[] { "tenant_id", "funding_financial_account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fixed_deposits_tenant_id_status",
                schema: "finance",
                table: "fixed_deposits",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_fixed_deposits_tenant_id_certificate_number",
                schema: "finance",
                table: "fixed_deposits",
                columns: new[] { "tenant_id", "certificate_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "fixed_deposit_interest_accruals",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "fixed_deposit_interest_receipts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "fixed_deposits",
                schema: "finance");
        }
    }

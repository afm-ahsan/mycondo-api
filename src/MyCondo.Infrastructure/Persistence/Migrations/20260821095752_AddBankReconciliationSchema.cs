using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class AddBankReconciliationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_reconciliations",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    statement_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    opening_ledger_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reconciled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconciled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_reconciliations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_statement_lines",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_reconciliation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    matched_ledger_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    adjustment_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_lines", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_reconciliations_tenant_id_financial_account_id_statement_date",
                schema: "finance",
                table: "bank_reconciliations",
                columns: new[] { "tenant_id", "financial_account_id", "statement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_tenant_id_bank_reconciliation_id",
                schema: "finance",
                table: "bank_statement_lines",
                columns: new[] { "tenant_id", "bank_reconciliation_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_reconciliations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "bank_statement_lines",
                schema: "finance");
        }
    }

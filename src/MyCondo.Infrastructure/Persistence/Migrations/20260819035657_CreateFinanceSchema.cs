using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class CreateFinanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ledger_postings_tenant_id_reference_type_reference_id",
                schema: "payments",
                table: "ledger_postings");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.AddColumn<Guid>(
                name: "accounting_period_id",
                schema: "payments",
                table: "ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "chart_of_account_id",
                schema: "payments",
                table: "ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fund_id",
                schema: "payments",
                table: "ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "account_mappings",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posting_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    chart_of_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounting_periods",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    normal_balance = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    parent_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_system_account = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chart_of_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_years",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "funds",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ledger_postings_tenant_id_reference_type_reference_id",
                schema: "payments",
                table: "ledger_postings",
                columns: new[] { "tenant_id", "reference_type", "reference_id" },
                unique: true,
                filter: "reference_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_account_mappings_tenant_id_posting_role",
                schema: "finance",
                table: "account_mappings",
                columns: new[] { "tenant_id", "posting_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_tenant_id_financial_year_id",
                schema: "finance",
                table: "accounting_periods",
                columns: new[] { "tenant_id", "financial_year_id" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_tenant_id_start_date_end_date",
                schema: "finance",
                table: "accounting_periods",
                columns: new[] { "tenant_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ux_chart_of_accounts_tenant_id_code",
                schema: "finance",
                table: "chart_of_accounts",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_years_tenant_id_start_date_end_date",
                schema: "finance",
                table: "financial_years",
                columns: new[] { "tenant_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ux_funds_tenant_id_code",
                schema: "finance",
                table: "funds",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_mappings",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "accounting_periods",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "chart_of_accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_years",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "funds",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "ux_ledger_postings_tenant_id_reference_type_reference_id",
                schema: "payments",
                table: "ledger_postings");

            migrationBuilder.DropColumn(
                name: "accounting_period_id",
                schema: "payments",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "chart_of_account_id",
                schema: "payments",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "fund_id",
                schema: "payments",
                table: "ledger_entries");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_postings_tenant_id_reference_type_reference_id",
                schema: "payments",
                table: "ledger_postings",
                columns: new[] { "tenant_id", "reference_type", "reference_id" });
        }
    }

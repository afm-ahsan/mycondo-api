using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddExpenseAccountingIntegration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "building_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<DateOnly>(
            name: "accounting_date",
            schema: "expenses",
            table: "expenses",
            type: "date",
            nullable: false,
            defaultValue: new DateOnly(1, 1, 1));

        // Existing rows: accounting date defaults to the expense date they already have — never
        // fabricate a different value for historical rows (see Expense.AccountingDate's doc comment).
        migrationBuilder.Sql("UPDATE expenses.expenses SET accounting_date = expense_date;");

        migrationBuilder.AddColumn<Guid>(
            name: "financial_account_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "fund_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_paid",
            schema: "expenses",
            table: "expenses",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "payment_posting_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "payment_reversal_posting_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "posting_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "reversal_posting_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "expense_category_id",
            schema: "expenses",
            table: "expense_types",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "expense_categories",
            schema: "expenses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_expense_categories", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_fund_id",
            schema: "expenses",
            table: "expenses",
            columns: new[] { "tenant_id", "fund_id" });

        migrationBuilder.CreateIndex(
            name: "ix_expense_types_tenant_id_expense_category_id",
            schema: "expenses",
            table: "expense_types",
            columns: new[] { "tenant_id", "expense_category_id" });

        migrationBuilder.CreateIndex(
            name: "ux_expense_categories_tenant_id_code",
            schema: "expenses",
            table: "expense_categories",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_expense_categories_tenant_id_name",
            schema: "expenses",
            table: "expense_categories",
            columns: new[] { "tenant_id", "name" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "expense_categories",
            schema: "expenses");

        migrationBuilder.DropIndex(
            name: "ix_expenses_tenant_id_fund_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropIndex(
            name: "ix_expense_types_tenant_id_expense_category_id",
            schema: "expenses",
            table: "expense_types");

        migrationBuilder.DropColumn(
            name: "accounting_date",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "financial_account_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "fund_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "is_paid",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "payment_posting_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "payment_reversal_posting_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "posting_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "reversal_posting_id",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "expense_category_id",
            schema: "expenses",
            table: "expense_types");

        migrationBuilder.AlterColumn<Guid>(
            name: "building_id",
            schema: "expenses",
            table: "expenses",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}

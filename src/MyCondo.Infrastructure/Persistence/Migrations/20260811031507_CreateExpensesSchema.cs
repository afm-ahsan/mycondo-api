using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CreateExpensesSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "expenses");

        migrationBuilder.CreateTable(
            name: "expense_types",
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
                table.PrimaryKey("pk_expense_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "expenses",
            schema: "expenses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                building_id = table.Column<Guid>(type: "uuid", nullable: false),
                expense_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                payee = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_expenses", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_expense_types_tenant_id_code",
            schema: "expenses",
            table: "expense_types",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_expense_types_tenant_id_name",
            schema: "expenses",
            table: "expense_types",
            columns: new[] { "tenant_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_building_id",
            schema: "expenses",
            table: "expenses",
            columns: new[] { "tenant_id", "building_id" });

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_expense_date",
            schema: "expenses",
            table: "expenses",
            columns: new[] { "tenant_id", "expense_date" });

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_expense_type_id",
            schema: "expenses",
            table: "expenses",
            columns: new[] { "tenant_id", "expense_type_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "expense_types",
            schema: "expenses");

        migrationBuilder.DropTable(
            name: "expenses",
            schema: "expenses");
    }
}

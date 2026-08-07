using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Operations_CylinderStock_Reconciliations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cylinder_stock_movements",
            schema: "operations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                cylinder_purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cylinder_stock_movements", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "monthly_cylinder_reconciliations",
            schema: "operations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                period_month = table.Column<DateOnly>(type: "date", nullable: false),
                opening_stock = table.Column<int>(type: "integer", nullable: false),
                total_received = table.Column<int>(type: "integer", nullable: false),
                total_issued = table.Column<int>(type: "integer", nullable: false),
                total_empty_returned = table.Column<int>(type: "integer", nullable: false),
                closing_stock = table.Column<int>(type: "integer", nullable: false),
                variance_quantity = table.Column<int>(type: "integer", nullable: false),
                remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                reconciled_by = table.Column<Guid>(type: "uuid", nullable: true),
                reconciled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_monthly_cylinder_reconciliations", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_cylinder_stock_movements_tenant_id_cylinder_type_occurred_at_utc",
            schema: "operations",
            table: "cylinder_stock_movements",
            columns: new[] { "tenant_id", "cylinder_type", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_monthly_cylinder_reconciliations_tenant_id_cylinder_type_period_month",
            schema: "operations",
            table: "monthly_cylinder_reconciliations",
            columns: new[] { "tenant_id", "cylinder_type", "period_month" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cylinder_stock_movements",
            schema: "operations");

        migrationBuilder.DropTable(
            name: "monthly_cylinder_reconciliations",
            schema: "operations");
    }
}

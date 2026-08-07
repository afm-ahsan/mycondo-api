using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Operations_GasCylinder_Suppliers_Purchases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cylinder_purchases",
            schema: "operations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                cylinder_weight_kg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                rate_per_cylinder = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                delivery_or_other_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                payment_status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cylinder_purchases", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "gas_cylinder_suppliers",
            schema: "operations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_gas_cylinder_suppliers", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_cylinder_purchases_tenant_id_supplier_id_purchase_date",
            schema: "operations",
            table: "cylinder_purchases",
            columns: new[] { "tenant_id", "supplier_id", "purchase_date" });

        migrationBuilder.CreateIndex(
            name: "ix_gas_cylinder_suppliers_tenant_id_is_active",
            schema: "operations",
            table: "gas_cylinder_suppliers",
            columns: new[] { "tenant_id", "is_active" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cylinder_purchases",
            schema: "operations");

        migrationBuilder.DropTable(
            name: "gas_cylinder_suppliers",
            schema: "operations");
    }
}

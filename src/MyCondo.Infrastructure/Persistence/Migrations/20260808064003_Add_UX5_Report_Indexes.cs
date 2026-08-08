using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_UX5_Report_Indexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_readings_tenant_id_building_id_utility_type_status",
            schema: "utilities",
            table: "readings",
            columns: new[] { "tenant_id", "building_id", "utility_type", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_payments_tenant_id_status_business_date",
            schema: "payments",
            table: "payments",
            columns: new[] { "tenant_id", "status", "business_date" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_readings_tenant_id_building_id_utility_type_status",
            schema: "utilities",
            table: "readings");

        migrationBuilder.DropIndex(
            name: "ix_payments_tenant_id_status_business_date",
            schema: "payments",
            table: "payments");
    }
}

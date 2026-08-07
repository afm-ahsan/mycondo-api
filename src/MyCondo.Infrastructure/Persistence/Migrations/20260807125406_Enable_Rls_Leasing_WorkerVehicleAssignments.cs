using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the two new Priority-2 `leasing` schema tables
/// (worker/vehicle assignment links) — same pattern as <c>Enable_Rls_Leasing</c>.
/// </summary>
public partial class Enable_Rls_Leasing_WorkerVehicleAssignments : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "occupancy_registration_worker_assignments",
        "occupancy_registration_vehicle_assignments",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE leasing.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE leasing.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON leasing.{table}
                    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables.Reverse())
        {
            migrationBuilder.Sql(
                $"""
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON leasing.{table};
                ALTER TABLE leasing.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE leasing.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

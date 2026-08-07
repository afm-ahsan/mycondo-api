using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the new `leasing` schema tables (Tenant Registration /
/// Occupancy Registration), following the same pattern as every prior RLS migration — see
/// Enable_Rls_Operations_Generators.
/// </summary>
public partial class Enable_Rls_Leasing : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "occupancy_registrations",
        "household_members",
        "occupancy_registration_status_histories",
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

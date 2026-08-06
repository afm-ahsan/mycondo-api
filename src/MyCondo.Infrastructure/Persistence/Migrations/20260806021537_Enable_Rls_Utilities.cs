using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the Slice F `utilities` schema tables (meters,
/// meter_assignments, rate_plans, rate_slabs, readings), following the same pattern as every prior
/// RLS migration — see mycondo-api/.claude/skills/postgresql-rls.md.
/// </summary>
public partial class Enable_Rls_Utilities : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "meters",
        "meter_assignments",
        "rate_plans",
        "rate_slabs",
        "readings",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE utilities.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE utilities.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON utilities.{table}
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
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON utilities.{table};
                ALTER TABLE utilities.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE utilities.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

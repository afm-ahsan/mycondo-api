using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the last two Slice H `operations` schema tables
/// (cylinder_stock_movements, monthly_cylinder_reconciliations), completing RLS coverage for every
/// table this slice added. Same pattern as every prior RLS migration.
/// </summary>
public partial class Enable_Rls_Operations_CylinderStock_Reconciliations : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "cylinder_stock_movements",
        "monthly_cylinder_reconciliations",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE operations.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE operations.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON operations.{table}
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
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON operations.{table};
                ALTER TABLE operations.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE operations.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

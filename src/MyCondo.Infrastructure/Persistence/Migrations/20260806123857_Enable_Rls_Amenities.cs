using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the Slice G `amenities` schema tables (facilities,
/// blackout_dates, bookings, pool_sessions, pool_incidents), following the same pattern as every prior
/// RLS migration — see mycondo-api/.claude/skills/postgresql-rls.md.
/// </summary>
public partial class Enable_Rls_Amenities : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "facilities",
        "blackout_dates",
        "bookings",
        "pool_sessions",
        "pool_incidents",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE amenities.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE amenities.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON amenities.{table}
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
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON amenities.{table};
                ALTER TABLE amenities.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE amenities.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

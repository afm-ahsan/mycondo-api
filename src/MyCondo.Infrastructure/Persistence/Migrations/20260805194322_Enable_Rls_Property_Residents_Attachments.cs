using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the Slice A tenant-scoped tables, following the exact
/// pattern established by <c>Enable_Tenant_Row_Level_Security</c> for the identity schema (see
/// mycondo-api/.claude/skills/postgresql-rls.md) — FORCE is required because the connecting migrator
/// role owns these tables, and NULLIF(...,'')::uuid avoids the ''::uuid cast error for anonymous/no-
/// tenant-context requests.
/// </summary>
public partial class Enable_Rls_Property_Residents_Attachments : Migration
{
    private static readonly (string Schema, string Table)[] TenantScopedTables =
    [
        ("property", "buildings"),
        ("property", "flats"),
        ("property", "gates"),
        ("residents", "residents"),
        ("documents", "attachments"),
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach ((string schema, string table) in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {schema}.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {schema}.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON {schema}.{table}
                    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach ((string schema, string table) in TenantScopedTables.Reverse())
        {
            migrationBuilder.Sql(
                $"""
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON {schema}.{table};
                ALTER TABLE {schema}.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE {schema}.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

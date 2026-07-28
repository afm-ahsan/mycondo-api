using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on every tenant-scoped identity table, closing the gap
/// flagged in mycondo-docs ADR-009. FORCE is required (not optional) because there is only one
/// Postgres role in this setup (mycondo_app) — it owns the tables it creates via migrations, and
/// table owners bypass RLS unless FORCE is also set.
///
/// The policy expression uses NULLIF(current_setting(...), '')::uuid rather than a direct cast:
/// TenantContextConnectionInterceptor always sets app.current_tenant_id, using '' (never a bare
/// skip/NULL) when there's no current tenant. ''::uuid raises a Postgres error, so NULLIF converts
/// both "never set" and "explicitly cleared" to NULL first, making the comparison evaluate to NULL
/// (no visible rows) instead of throwing.
/// </summary>
public partial class Enable_Tenant_Row_Level_Security : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "users",
        "roles",
        "role_assignments",
        "refresh_tokens",
        "role_permissions",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE identity.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON identity.{table}
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
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON identity.{table};
                ALTER TABLE identity.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE identity.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

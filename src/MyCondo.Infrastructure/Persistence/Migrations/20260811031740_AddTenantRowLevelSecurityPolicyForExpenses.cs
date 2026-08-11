using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on the
/// new <c>expenses.expense_types</c>/<c>expenses.expenses</c> tables — a new table gets its own new RLS
/// migration rather than editing the immutable clean-baseline <c>AddTenantRowLevelSecurityPolicies</c>
/// migration (mycondo-docs ADR-024; see mycondo-api/.claude/skills/postgresql-rls.md).
/// </summary>
public partial class AddTenantRowLevelSecurityPolicyForExpenses : Migration
{
    private static readonly (string Schema, string Table)[] TenantScopedTables =
    [
        ("expenses", "expense_types"),
        ("expenses", "expenses"),
    ];

    /// <inheritdoc />
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

    /// <inheritdoc />
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

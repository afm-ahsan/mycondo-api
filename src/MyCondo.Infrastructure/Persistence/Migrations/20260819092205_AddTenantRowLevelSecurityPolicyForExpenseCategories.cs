using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on the
/// new <c>expenses.expense_categories</c> table (Template 3) — a new table gets its own new RLS
/// migration rather than editing the immutable clean-baseline migration (mycondo-docs ADR-024; see
/// mycondo-api/.claude/skills/postgresql-rls.md), same pattern as
/// <c>AddTenantRowLevelSecurityPolicyForExpenses</c>. No new Grant-privileges migration is needed — the
/// <c>expenses</c> schema's runtime grants (from the original baseline) already cover every table
/// created in that schema via <c>ALTER DEFAULT PRIVILEGES</c>.
/// </summary>
public partial class AddTenantRowLevelSecurityPolicyForExpenseCategories : Migration
{
    private const string Schema = "expenses";
    private const string Table = "expense_categories";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            $"""
            ALTER TABLE {Schema}.{Table} ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {Schema}.{Table} FORCE ROW LEVEL SECURITY;

            CREATE POLICY rls_{Table}_tenant_isolation ON {Schema}.{Table}
                USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            $"""
            DROP POLICY IF EXISTS rls_{Table}_tenant_isolation ON {Schema}.{Table};
            ALTER TABLE {Schema}.{Table} NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE {Schema}.{Table} DISABLE ROW LEVEL SECURITY;
            """);
    }
}

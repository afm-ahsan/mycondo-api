using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on the new
/// <c>finance.financial_accounts</c>/<c>fixed_deposits</c>/<c>fixed_deposit_interest_accruals</c>/
/// <c>fixed_deposit_interest_receipts</c> tables (Template 4) — a new table gets its own new RLS
/// migration rather than editing the immutable clean-baseline migration (mycondo-docs ADR-024; see
/// mycondo-api/.claude/skills/postgresql-rls.md), same pattern as
/// <c>AddTenantRowLevelSecurityPolicyForFinance</c>. No new Grant-privileges migration is needed — the
/// <c>finance</c> schema's runtime grants (from <c>GrantAppRoleRuntimePrivilegesForFinance</c>) already
/// cover every table created in that schema via <c>ALTER DEFAULT PRIVILEGES</c>.
/// </summary>
public partial class AddTenantRowLevelSecurityPolicyForBankingFixedDeposits : Migration
{
    private static readonly (string Schema, string Table)[] TenantScopedTables =
    [
        ("finance", "financial_accounts"),
        ("finance", "fixed_deposits"),
        ("finance", "fixed_deposit_interest_accruals"),
        ("finance", "fixed_deposit_interest_receipts"),
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

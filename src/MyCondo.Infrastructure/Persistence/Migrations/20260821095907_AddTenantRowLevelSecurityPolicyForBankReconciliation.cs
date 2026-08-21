using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <summary>
    /// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on the
    /// new <c>finance.bank_reconciliations</c>/<c>bank_statement_lines</c> tables (Template 6) — a new
    /// table gets its own new RLS migration rather than editing the immutable clean-baseline migration
    /// (mycondo-docs ADR-024; see mycondo-api/.claude/skills/postgresql-rls.md), same pattern as
    /// <c>AddTenantRowLevelSecurityPolicyForBankingFixedDeposits</c>. No new Grant-privileges migration
    /// is needed — the <c>finance</c> schema's runtime grants (from
    /// <c>GrantAppRoleRuntimePrivilegesForFinance</c>) already cover every table created in that schema
    /// via <c>ALTER DEFAULT PRIVILEGES</c>.
    /// </summary>
    public partial class AddTenantRowLevelSecurityPolicyForBankReconciliation : Migration
    {
        private static readonly (string Schema, string Table)[] TenantScopedTables =
        [
            ("finance", "bank_reconciliations"),
            ("finance", "bank_statement_lines"),
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

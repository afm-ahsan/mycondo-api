using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <summary>
    /// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on the
    /// new <c>finance.finance_audit_log</c> table (Template 6) — a new table gets its own new RLS
    /// migration rather than editing the immutable clean-baseline migration (mycondo-docs ADR-024; see
    /// mycondo-api/.claude/skills/postgresql-rls.md), same pattern as
    /// <c>AddTenantRowLevelSecurityPolicyForBankingFixedDeposits</c>. No new Grant-privileges migration
    /// is needed — the <c>finance</c> schema's runtime grants (from
    /// <c>GrantAppRoleRuntimePrivilegesForFinance</c>) already cover every table created in that schema
    /// via <c>ALTER DEFAULT PRIVILEGES</c>.
    /// </summary>
    public partial class AddTenantRowLevelSecurityPolicyForFinanceAuditLog : Migration
    {
        private const string Schema = "finance";
        private const string Table = "finance_audit_log";

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

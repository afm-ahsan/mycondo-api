using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the Slice E `billing` schema tables (service_charge_rules,
/// invoices, invoice_lines, invoice_sequences), following the same pattern as every prior RLS
/// migration — see mycondo-api/.claude/skills/postgresql-rls.md. invoice_sequences has no EF entity
/// mapping but is tenant-scoped, so RLS applies for consistency even though it's an internal counter.
/// </summary>
public partial class Enable_Rls_Billing : Migration
{
    private static readonly string[] TenantScopedTables =
    [
        "service_charge_rules",
        "invoices",
        "invoice_lines",
        "invoice_sequences",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string table in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE billing.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE billing.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON billing.{table}
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
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON billing.{table};
                ALTER TABLE billing.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE billing.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

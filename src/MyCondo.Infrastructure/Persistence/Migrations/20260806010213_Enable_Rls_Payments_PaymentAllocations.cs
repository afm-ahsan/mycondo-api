using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the Slice E payment_allocations table, following the
/// same pattern as every prior RLS migration — see mycondo-api/.claude/skills/postgresql-rls.md.
/// </summary>
public partial class Enable_Rls_Payments_PaymentAllocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE payments.payment_allocations ENABLE ROW LEVEL SECURITY;
            ALTER TABLE payments.payment_allocations FORCE ROW LEVEL SECURITY;

            CREATE POLICY rls_payment_allocations_tenant_isolation ON payments.payment_allocations
                USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS rls_payment_allocations_tenant_isolation ON payments.payment_allocations;
            ALTER TABLE payments.payment_allocations NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE payments.payment_allocations DISABLE ROW LEVEL SECURITY;
            """);
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security on the new `property.flat_ownerships` table (Phase 3,
/// mycondo-docs ADR-021), following the same pattern as every prior RLS migration — see
/// Enable_Rls_Leasing.
/// </summary>
public partial class Enable_Rls_FlatOwnerships : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE property.flat_ownerships ENABLE ROW LEVEL SECURITY;
            ALTER TABLE property.flat_ownerships FORCE ROW LEVEL SECURITY;

            CREATE POLICY rls_flat_ownerships_tenant_isolation ON property.flat_ownerships
                USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS rls_flat_ownerships_tenant_isolation ON property.flat_ownerships;
            ALTER TABLE property.flat_ownerships NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE property.flat_ownerships DISABLE ROW LEVEL SECURITY;
            """);
    }
}

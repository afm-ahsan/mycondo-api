using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted `mycondo_app` runtime role DML privileges on the new `platform` schema
/// (added by <c>Add_Platform_Identity_Tables</c>) — same shape as
/// <c>Grant_App_Role_Runtime_Privileges_Operations</c>/<c>_Leasing</c>. Runs as `mycondo_migrator`
/// (the DDL/owner role), which is what makes the grants and `ALTER DEFAULT PRIVILEGES` below valid.
///
/// Found missing during the Phase 1 live PostgreSQL verification (see
/// mycondo-phase1-final-postgresql-rls-verification-prompt.md): Add_Platform_Identity_Tables created
/// the schema/tables but never granted mycondo_app anything on them, so the running API — which
/// always connects as the restricted mycondo_app role, never mycondo_migrator (see
/// mycondo-api/CLAUDE.md's Multi-tenancy section) — would get a permission-denied error the first
/// time PlatformBootstrapSeeder or any Platform endpoint touched platform.platform_users et al. This
/// is a genuine Phase-1 defect fix, not a Phase-2/3 change: it grants exactly the same DML shape every
/// other schema already has, on the schema Phase 1 itself introduced. No RLS is added here — the
/// platform schema deliberately remains outside RLS (see mycondo-docs ADR-019); this migration only
/// grants ordinary DML, identical in kind to what every other schema's app-role grant already does.
/// </summary>
public partial class Grant_App_Role_Runtime_Privileges_Platform : Migration
{
    private const string Schema = "platform";

    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            $"""
            GRANT USAGE ON SCHEMA {Schema} TO mycondo_app;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {Schema} TO mycondo_app;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {Schema} TO mycondo_app;
            ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {Schema}
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO mycondo_app;
            ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {Schema}
                GRANT USAGE, SELECT ON SEQUENCES TO mycondo_app;
            """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            $"""
            ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {Schema}
                REVOKE USAGE, SELECT ON SEQUENCES FROM mycondo_app;
            ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {Schema}
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM mycondo_app;
            REVOKE ALL ON ALL SEQUENCES IN SCHEMA {Schema} FROM mycondo_app;
            REVOKE ALL ON ALL TABLES IN SCHEMA {Schema} FROM mycondo_app;
            REVOKE USAGE ON SCHEMA {Schema} FROM mycondo_app;
            """);
}

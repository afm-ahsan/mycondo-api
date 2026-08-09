using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted `mycondo_app` runtime role DML privileges on the `utilities` schema (added by
/// <c>Add_Utilities_Meters_RatePlans_Readings</c>, Slice F, 2026-08-06) — same shape as
/// <c>Grant_App_Role_Runtime_Privileges_Operations</c>/<c>_Leasing</c>/<c>_Platform</c>. Runs as
/// `mycondo_migrator` (the DDL/owner role), which is what makes the grants and
/// `ALTER DEFAULT PRIVILEGES` below valid.
///
/// Found missing during the Phase 1 live PostgreSQL verification (see
/// mycondo-phase1-final-postgresql-rls-verification-prompt.md) while running the pre-existing
/// `MyCondo.MultiTenancyTests.UtilitiesCrossTenantIsolationTests` suite against a freshly, fully
/// migrated database. This is a PRE-EXISTING defect from Slice F, entirely unrelated to Phase 1 —
/// `Add_Utilities_Meters_RatePlans_Readings` created the schema via `EnsureSchema` but no follow-up
/// grant migration was ever added for it (unlike `operations`/`leasing`, which each got one). The real
/// `mycondo` development database has the identical gap (confirmed read-only, not modified by this
/// migration file's authoring). Fixed here because it otherwise fails
/// `UtilitiesCrossTenantIsolationTests` unconditionally — with "permission denied for schema
/// utilities" — regardless of Phase 1, blocking the "existing tenant RLS tests remain green"
/// verification requirement. This migration only grants ordinary DML, identical in kind and scope to
/// every other schema's app-role grant; it changes no RLS policy and no Phase-1/Platform behavior.
/// </summary>
public partial class Grant_App_Role_Runtime_Privileges_Utilities : Migration
{
    private const string Schema = "utilities";

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

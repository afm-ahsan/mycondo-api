using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted `mycondo_app` runtime role DML privileges on the new `operations` schema
/// (added by <c>Add_Operations_Generators</c>), exact same shape as
/// <c>Grant_App_Role_Runtime_Privileges</c> (2026-07-28) — that migration's `ALTER DEFAULT PRIVILEGES`
/// only covers schemas that existed at the time it ran, so a newly added schema needs its own grant.
/// Runs as `mycondo_migrator` (the DDL/owner role), which is what makes the grants and
/// `ALTER DEFAULT PRIVILEGES` below valid.
/// </summary>
public partial class Grant_App_Role_Runtime_Privileges_Operations : Migration
{
    private const string Schema = "operations";

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

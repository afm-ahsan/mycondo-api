using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted `mycondo_app` runtime role DML privileges on the new `leasing` schema
/// (added by <c>Add_Leasing_OccupancyRegistrations</c>) — same shape as
/// <c>Grant_App_Role_Runtime_Privileges_Operations</c>.
/// </summary>
public partial class Grant_App_Role_Runtime_Privileges_Leasing : Migration
{
    private const string Schema = "leasing";

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

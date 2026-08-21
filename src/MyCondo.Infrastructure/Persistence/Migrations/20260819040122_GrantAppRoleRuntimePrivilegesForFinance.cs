using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted <c>mycondo_app</c> runtime role (ADR-016) DML privileges on the new
/// <c>finance</c> schema — the consolidated baseline (<c>GrantAppRoleRuntimePrivileges</c>) only covers
/// the schema list that existed as of 2026-08-09, which predates this schema, so it needs its own
/// follow-up grant migration (same shape every other post-baseline schema addition needs — see
/// <c>GrantAppRoleRuntimePrivileges</c>'s own doc comment). Part of the Finance Foundation (ADR-027).
/// </summary>
public partial class GrantAppRoleRuntimePrivilegesForFinance : Migration
{
    private const string Schema = "finance";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
}

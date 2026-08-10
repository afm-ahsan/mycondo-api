using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted <c>mycondo_app</c> runtime role (ADR-016 — DML-only, non-superuser, owns
/// nothing) DML privileges on every schema in the clean baseline — consolidated from the 5 historical
/// <c>Grant_App_Role_Runtime_Privileges*</c> migrations this baseline replaces. Runs as
/// <c>mycondo_migrator</c> (the DDL/owner role), which is what makes the grants and
/// <c>ALTER DEFAULT PRIVILEGES</c> below valid — see mycondo-api/CLAUDE.md's Multi-tenancy section for
/// why the running application never connects as <c>mycondo_migrator</c>. <c>ALTER DEFAULT PRIVILEGES</c>
/// ensures a table/sequence created by a *later* migration (which also runs as <c>mycondo_migrator</c>)
/// is automatically grantable to <c>mycondo_app</c> without a follow-up grant migration for routine
/// schema evolution — mirroring exactly what the historical migrations already did per-schema.
/// </summary>
public partial class GrantAppRoleRuntimePrivileges : Migration
{
    private static readonly string[] Schemas =
    [
        "tenancy",
        "identity",
        "property",
        "residents",
        "leasing",
        "billing",
        "payments",
        "expenses",
        "vendors",
        "payroll",
        "complaints",
        "maintenance",
        "amenities",
        "security",
        "notifications",
        "documents",
        "reporting",
        "audit",
        "operations",
        "platform",
        "utilities",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string schema in Schemas)
        {
            migrationBuilder.Sql(
                $"""
                GRANT USAGE ON SCHEMA {schema} TO mycondo_app;
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {schema} TO mycondo_app;
                GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {schema} TO mycondo_app;
                ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {schema}
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO mycondo_app;
                ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {schema}
                    GRANT USAGE, SELECT ON SEQUENCES TO mycondo_app;
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (string schema in Schemas.Reverse())
        {
            migrationBuilder.Sql(
                $"""
                ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {schema}
                    REVOKE USAGE, SELECT ON SEQUENCES FROM mycondo_app;
                ALTER DEFAULT PRIVILEGES FOR ROLE mycondo_migrator IN SCHEMA {schema}
                    REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM mycondo_app;
                REVOKE ALL ON ALL SEQUENCES IN SCHEMA {schema} FROM mycondo_app;
                REVOKE ALL ON ALL TABLES IN SCHEMA {schema} FROM mycondo_app;
                REVOKE USAGE ON SCHEMA {schema} FROM mycondo_app;
                """);
        }
    }
}

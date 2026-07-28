using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Grants the restricted `mycondo_app` runtime role DML privileges on every MyCondo schema, without
/// making it an owner of anything. This migration runs as `mycondo_migrator` (the DDL/owner role —
/// see docker-compose.yml and README.md's migration instructions), which is what makes the grants and
/// `ALTER DEFAULT PRIVILEGES` below valid: you can only set default privileges `FOR ROLE` yourself
/// (or as superuser), and the migrator connection is exactly that role.
///
/// This exists because RLS's `FORCE` was previously meaningless in practice: the original single-role
/// setup ran migrations *and* the app itself as the same role, which — being the initdb bootstrap role
/// for the container — was also a Postgres superuser. Superusers and BYPASSRLS roles always bypass row
/// security regardless of FORCE. See mycondo-docs' ADR recording this (follow-up to ADR-009) and
/// `mycondo-api/.claude/skills/postgresql-rls.md`.
///
/// The `ALTER DEFAULT PRIVILEGES` statements are what make this durable across future waves: any table
/// `mycondo_migrator` creates in these schemas from now on (billing, payments, etc. in later waves)
/// automatically grants `mycondo_app` the same DML rights, with nothing to remember per migration.
/// `mycondo_app` will own none of these objects, so RLS applies to it even without FORCE — FORCE stays
/// in place anyway as documented, explicit defense-in-depth.
/// </summary>
public partial class Grant_App_Role_Runtime_Privileges : Migration
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
    ];

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

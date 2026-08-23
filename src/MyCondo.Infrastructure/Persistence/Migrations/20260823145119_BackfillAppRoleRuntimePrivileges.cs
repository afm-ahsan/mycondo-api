using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Backfills <c>mycondo_app</c> DML privileges on every baseline schema for databases that already
/// applied <see cref="GrantAppRoleRuntimePrivileges"/> before its literal role name and its
/// <c>ALTER DEFAULT PRIVILEGES</c> target were corrected (that migration originally granted to a
/// stale <c>condobd_app</c> literal via <c>ALTER DEFAULT PRIVILEGES FOR ROLE condobd_app/mycondo_app</c>
/// — the wrong target: that clause governs objects the *named* role creates, not objects granted to
/// it, so it never actually applied to tables created afterwards as <c>mycondo_migrator</c>, e.g.
/// <c>expenses.expense_categories</c>). Fixing that migration's source doesn't retroactively re-run it
/// on a database where it's already recorded in <c>__ef_migrations_history</c> — hence this explicit,
/// idempotent, additive follow-up, the same shape as <c>GrantAppRoleRuntimePrivilegesForFinance</c>.
/// A freshly-bootstrapped database (Testcontainers, a new native install, a fresh Docker Compose
/// volume) never depends on this migration — the corrected baseline already grants everything
/// correctly from a clean start.
/// </summary>
public partial class BackfillAppRoleRuntimePrivileges : Migration
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
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately a no-op: these grants are also produced by the (now-corrected)
        // GrantAppRoleRuntimePrivileges.Up() and its own default-privileges clause for every table
        // created afterwards, so revoking here would strip privileges that migration still expects to
        // be in place on a partial rollback that stops short of reverting it too.
    }
}

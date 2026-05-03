using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Initial migration: creates the per-module PostgreSQL schemas used by MyCondo.
/// No tables yet — those land per module in subsequent migrations.
/// Schema-per-module is documented in docs/kickoff.md (Phase 1) and CLAUDE.md.
/// </summary>
public partial class InitialCreate : Migration
{
    private static readonly string[] Schemas =
    [
        "tenancy",        // Tenant Provisioning
        "identity",       // Auth, Roles, Permissions
        "property",       // Building / Tower / Floor / Unit
        "residents",      // Owners, Tenants, Ownership
        "leasing",        // Lease lifecycle
        "billing",        // Service charges, invoices
        "payments",       // Payments, allocations, ledger
        "expenses",       // Vendor bills, expenses
        "vendors",        // Vendor master, contracts
        "payroll",        // Staff, salaries, attendance
        "complaints",     // Tickets
        "maintenance",    // Work orders, schedules (P2)
        "amenities",      // Facility booking (P2)
        "security",       // Visitor management (P2)
        "notifications",  // Templates, dispatch log
        "documents",      // Files, versions
        "reporting",      // Materialized views, aggregates
        "audit"           // Cross-cutting change log (partitioned)
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (string schema in Schemas)
        {
            migrationBuilder.Sql($"CREATE SCHEMA IF NOT EXISTS {schema};");
        }

        // app.current_tenant_id is read by every RLS policy on tenant-scoped tables.
        // Initialize the GUC at the database level so connections without an explicit SET
        // get a NULL value rather than failing — RLS policies treat NULL as "no rows".
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                BEGIN
                    PERFORM set_config('app.current_tenant_id', '', false);
                EXCEPTION WHEN OTHERS THEN NULL;
                END;
            END$$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse order so dependent objects drop first if any leak through.
        foreach (string schema in Schemas.Reverse())
        {
            migrationBuilder.Sql($"DROP SCHEMA IF EXISTS {schema} CASCADE;");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enables and forces Row-Level Security, and creates the standard tenant-isolation policy, on every
/// tenant-scoped table in the clean baseline (<c>InitialPlatformSchema</c>) — consolidated from the 17
/// historical <c>Enable_Rls_*</c>/<c>Enable_Tenant_Row_Level_Security</c> migrations this baseline
/// replaces (mycondo-docs — clean migration baseline cleanup). Every entry below was independently
/// verified against those historical migrations before this consolidation; the policy shape itself is
/// unchanged — see mycondo-api/.claude/skills/postgresql-rls.md for the exact mechanics and the
/// <c>NULLIF(...,'')::uuid</c> rationale. <c>platform.*</c> tables deliberately have no entry here —
/// they hold no tenant-scoped data (mycondo-docs ADR-019).
/// </summary>
public partial class AddTenantRowLevelSecurityPolicies : Migration
{
    private static readonly (string Schema, string Table)[] TenantScopedTables =
    [
        // identity
        ("identity", "users"),
        ("identity", "roles"),
        ("identity", "role_assignments"),
        ("identity", "refresh_tokens"),
        ("identity", "role_permissions"),

        // property
        ("property", "buildings"),
        ("property", "flats"),
        ("property", "gates"),
        ("property", "flat_ownerships"),

        // residents
        ("residents", "residents"),

        // documents
        ("documents", "attachments"),

        // security
        ("security", "guest_profiles"),
        ("security", "vehicles"),
        ("security", "access_sessions"),
        ("security", "domestic_worker_profiles"),
        ("security", "domestic_worker_assignments"),
        ("security", "service_provider_profiles"),
        ("security", "service_provider_assignments"),
        ("security", "seba_visit_details"),
        ("security", "parcels"),
        ("security", "parcel_custody_events"),

        // payroll
        ("payroll", "staff_members"),
        ("payroll", "attendance_records"),

        // payments
        ("payments", "idempotency_keys"),
        ("payments", "ledger_entries"),
        ("payments", "ledger_postings"),
        ("payments", "payments"),
        ("payments", "resident_accounts"),
        ("payments", "payment_allocations"),

        // billing
        ("billing", "service_charge_rules"),
        ("billing", "invoices"),
        ("billing", "invoice_lines"),
        ("billing", "invoice_sequences"),

        // utilities
        ("utilities", "meters"),
        ("utilities", "meter_assignments"),
        ("utilities", "rate_plans"),
        ("utilities", "rate_slabs"),
        ("utilities", "readings"),

        // amenities
        ("amenities", "facilities"),
        ("amenities", "blackout_dates"),
        ("amenities", "bookings"),
        ("amenities", "pool_sessions"),
        ("amenities", "pool_incidents"),

        // operations
        ("operations", "generators"),
        ("operations", "generator_sessions"),
        ("operations", "generator_fuel_receipts"),
        ("operations", "generator_maintenance_schedules"),
        ("operations", "generator_service_records"),
        ("operations", "generator_breakdown_records"),
        ("operations", "gas_cylinder_suppliers"),
        ("operations", "cylinder_purchases"),
        ("operations", "cylinder_stock_movements"),
        ("operations", "monthly_cylinder_reconciliations"),

        // leasing
        ("leasing", "occupancy_registrations"),
        ("leasing", "household_members"),
        ("leasing", "occupancy_registration_status_histories"),
        ("leasing", "occupancy_registration_worker_assignments"),
        ("leasing", "occupancy_registration_vehicle_assignments"),
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach ((string schema, string table) in TenantScopedTables)
        {
            migrationBuilder.Sql(
                $"""
                ALTER TABLE {schema}.{table} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {schema}.{table} FORCE ROW LEVEL SECURITY;

                CREATE POLICY rls_{table}_tenant_isolation ON {schema}.{table}
                    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach ((string schema, string table) in TenantScopedTables.Reverse())
        {
            migrationBuilder.Sql(
                $"""
                DROP POLICY IF EXISTS rls_{table}_tenant_isolation ON {schema}.{table};
                ALTER TABLE {schema}.{table} NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE {schema}.{table} DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}

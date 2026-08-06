using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the permissions Slice D (Financial Foundation) needs that didn't already exist.
/// residentaccount.manage covers opening accounts and recording opening balances.
///
/// Originally also re-inserted payment.view, payment.record, and payment.reverse — but those 3
/// names were already seeded by the original Wave 0 Seed_Permission_Catalogue (a speculative
/// placeholder entry for a payments feature that hadn't been built yet), so the unconditional
/// InsertData failed with a unique-constraint violation on ux_permissions_name on any database that
/// actually ran this migration from a clean state. This went undetected until the first real
/// Postgres-backed migration run in this engagement (Docker/Testcontainers had been unavailable the
/// whole time) — confirmed unapplied everywhere and safe to correct in place per the standing
/// instruction to fix genuinely defective, unapplied migrations rather than layer a workaround on
/// top. The 3 duplicate lines are removed here; the original catalogue's rows for those 3 names
/// (same module/scoping, adequate descriptions) remain the single source of truth for them.
/// </summary>
public partial class Seed_Payments_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("537f62c4-bdbb-4024-830a-1aea7afb5a03"), "View resident account details and ledger history", "residentaccount", true, "residentaccount.view" },
                { new Guid("d4d9d017-cb69-404e-a3d5-30e4fbc577fd"), "Open resident accounts and record opening balances", "residentaccount", true, "residentaccount.manage" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'residentaccount.view', 'residentaccount.manage'
            );
            """);
    }
}

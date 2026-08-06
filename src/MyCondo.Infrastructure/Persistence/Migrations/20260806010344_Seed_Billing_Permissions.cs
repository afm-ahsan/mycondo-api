using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the permissions Slice E (Service Charges, Invoice &amp; Billing) needs that didn't already
/// exist. Payment allocation (FIFO, inside RecordPaymentCommandHandler) needs no new permission —
/// it's already gated by Slice D's payment.record.
///
/// Originally also re-inserted billing.rule.view and billing.rule.manage — but those 2 names were
/// already seeded by the original Wave 0 Seed_Permission_Catalogue (a speculative placeholder entry
/// for a service-charge-rules feature that hadn't been built yet), so the unconditional InsertData
/// failed with a unique-constraint violation on ux_permissions_name on any database that actually ran
/// this migration from a clean state — same root cause and same fix rationale as
/// Seed_Payments_Permissions's equivalent correction. The 2 duplicate lines are removed here; the
/// original catalogue's rows for those 2 names remain the single source of truth for them.
/// </summary>
public partial class Seed_Billing_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("1729729e-17c3-4382-b1d7-a7af51c7564a"), "View invoices and invoice lines", "billing", true, "billing.invoice.view" },
                { new Guid("cc99282e-cf3f-4b8a-a58a-b2db87803290"), "Run invoice batch generation", "billing", true, "billing.invoice.generate" },
                { new Guid("57993c92-9ada-4fda-954f-a633fde76161"), "Void an unpaid invoice", "billing", true, "billing.invoice.void" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'billing.invoice.view', 'billing.invoice.generate', 'billing.invoice.void'
            );
            """);
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 5 permissions Slice E (Service Charges, Invoice &amp; Billing) needs — same rationale
/// as the prior Seed_*_Permissions migrations: these resources didn't exist in the original
/// catalogue. Payment allocation (FIFO, inside RecordPaymentCommandHandler) needs no new permission
/// — it's already gated by Slice D's payment.record.
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
                { new Guid("4fcec45f-aa9c-4f6b-bf45-83dd5a213e81"), "View service charge rules", "billing", true, "billing.rule.view" },
                { new Guid("bc35efae-6896-482a-80bc-3153dd3bb7a8"), "Create and end service charge rules", "billing", true, "billing.rule.manage" },
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
                'billing.rule.view', 'billing.rule.manage',
                'billing.invoice.view', 'billing.invoice.generate', 'billing.invoice.void'
            );
            """);
    }
}

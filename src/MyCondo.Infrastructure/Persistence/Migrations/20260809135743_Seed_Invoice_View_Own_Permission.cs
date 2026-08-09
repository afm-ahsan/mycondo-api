using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds a dedicated self-service permission for Phase 3's "My Invoices" endpoint (mycondo-docs
/// ADR-021) — deliberately NOT reusing the existing broad `invoice.view` (which also gates the admin
/// invoice-listing endpoint, covering every Flat in a Building/Tenant). Granting `invoice.view` to
/// FlatOwner/Tenant would let them call that admin endpoint too; this permission only ever gates the
/// caller's own relationship-derived Flat set.
/// </summary>
public partial class Seed_Invoice_View_Own_Permission : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("a39c4c40-6d32-4b93-9e88-bb1a4fe18beb"), "View own invoices via self-service (owned/occupied flats only)", "billing", true, "invoice.view.own" },
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions WHERE name = 'invoice.view.own';
            """);
    }
}

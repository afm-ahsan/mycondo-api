using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 11 permissions Slice H (Operations — Generator Management + Gas Cylinder Management)
/// needs, verbatim from the permission catalogue already published in
/// claude-mycondo-api-register-digitization-implementation.md before any Slice H code existed — same
/// rationale as every prior Seed_*_Permissions migration: these resources didn't exist in the original
/// catalogue. Seeded upfront (before the Gas Cylinder tables exist) since permissions are config data,
/// not entities — matches Seed_Facility_Pool_Permissions covering both Community Hall and Swimming
/// Pool in one migration even though they're separate features.
/// </summary>
public partial class Seed_Operations_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a01"), "View generators, sessions, fuel, and maintenance records", "generator", true, "generator.view" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a02"), "Create, edit, and deactivate generator assets", "generator", true, "generator.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a03"), "Start and stop a generator runtime session", "generator", true, "generator.operation.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a04"), "Record generator fuel receipts and reconciliation", "generator", true, "generator.fuel.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a05"), "Manage generator maintenance schedules, service records, and breakdowns", "generator", true, "generator.maintenance.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a06"), "View generator runtime, fuel, and maintenance reports", "generator", false, "generator.report" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a07"), "View gas cylinder suppliers, purchases, and stock", "gascylinder", true, "gascylinder.view" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a08"), "Record and manage gas cylinder suppliers and purchases", "gascylinder", true, "gascylinder.purchase.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a09"), "Record gas cylinder stock receipts, issues, empty returns, and reconciliations", "gascylinder", true, "gascylinder.stock.manage" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a10"), "Approve or reject a gas cylinder purchase, or authorize a controlled stock adjustment", "gascylinder", true, "gascylinder.approve" },
                { new Guid("0a6a5a9a-3e9a-4f0e-8a9a-1d6a4e9c1a11"), "View gas cylinder purchase, consumption, and supplier comparison reports", "gascylinder", false, "gascylinder.report" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'generator.view', 'generator.manage', 'generator.operation.manage',
                'generator.fuel.manage', 'generator.maintenance.manage', 'generator.report',
                'gascylinder.view', 'gascylinder.purchase.manage', 'gascylinder.stock.manage',
                'gascylinder.approve', 'gascylinder.report'
            );
            """);
    }
}

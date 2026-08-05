using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 13 permissions Slice B (Guest + Vehicle access) needs — none of the register
/// digitization spec's `visitor.*`/`vehicle.*` resources existed in the original 47-permission
/// catalogue (Seed_Permission_Catalogue), which only anticipated the original 11 MVP modules. Follows
/// the same flat `&lt;resource&gt;[.&lt;subresource&gt;].&lt;action&gt;` naming already established there,
/// not the register-digitization prompt's own 3-segment sketch. <c>report.security.view</c> extends
/// the existing `report.*` module family for the "currently inside" security report.
/// </summary>
public partial class Seed_Security_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("d0cbefee-8c7c-46ab-a5d4-96f4751e571c"), "View guest profiles and visit history", "visitor", true, "visitor.view" },
                { new Guid("8647bf8f-8c28-4e35-a2a7-b1240827a6fd"), "Create guest profiles", "visitor", true, "visitor.create" },
                { new Guid("d7d9aa6b-df85-4873-8fc9-ada8af204119"), "Check guests in", "visitor", true, "visitor.checkin" },
                { new Guid("9bc9428b-2f22-4499-9b10-45b899652b6c"), "Check guests out", "visitor", true, "visitor.checkout" },
                { new Guid("be5b99d5-9a03-465a-b346-f9fb69347410"), "Override a blocked guest's entry", "visitor", true, "visitor.override" },
                { new Guid("6fb73e2f-7a5b-412b-8f7f-89711e6d44da"), "Block and unblock guest profiles", "visitor", true, "visitor.block.manage" },
                { new Guid("5260e8c2-ed13-4e50-9d45-cff95cac5a96"), "View vehicles and trip history", "vehicle", true, "vehicle.view" },
                { new Guid("e0a372f1-4ebd-47c1-b420-882d4f65503b"), "Register vehicles", "vehicle", true, "vehicle.create" },
                { new Guid("d74058b5-b62d-41fd-ac81-8f87b85e191a"), "Check vehicles in", "vehicle", true, "vehicle.checkin" },
                { new Guid("df51ce68-5a63-4b93-8141-48a14fed60e4"), "Check vehicles out", "vehicle", true, "vehicle.checkout" },
                { new Guid("b393c2d4-d88a-4f7e-b1de-b0c0024f5182"), "Override a blocked vehicle's entry", "vehicle", true, "vehicle.override" },
                { new Guid("2c507bf0-5d80-4ac0-b909-143ea2f2a2bc"), "Block and unblock vehicles", "vehicle", true, "vehicle.block.manage" },
                { new Guid("0d1c8a64-b505-4efe-ac1c-97576ce080e5"), "View security reports (currently inside, gate activity)", "report", false, "report.security.view" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'visitor.view', 'visitor.create', 'visitor.checkin', 'visitor.checkout',
                'visitor.override', 'visitor.block.manage',
                'vehicle.view', 'vehicle.create', 'vehicle.checkin', 'vehicle.checkout',
                'vehicle.override', 'vehicle.block.manage',
                'report.security.view'
            );
            """);
    }
}

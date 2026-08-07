using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the Tenant Registration (domain name: Occupancy Registration — see
/// <c>OccupancyRegistration</c>'s doc comment for the naming rationale) permission catalogue: one
/// permission per stage of the two-stage approval workflow, matching the existing three-part/
/// stage-scoped naming convention (e.g. <c>gascylinder.approve</c>).
/// </summary>
public partial class Seed_Leasing_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("9a8900f2-c873-4910-8268-c9336fed476a"), "View tenant registrations, household members, and their status history", "occupancy-registration", true, "occupancy-registration.view" },
                { new Guid("6f87fa2a-6e81-494f-911b-d8046a166d5b"), "Create and edit a draft tenant registration, manage household members, and submit for review", "occupancy-registration", true, "occupancy-registration.create" },
                { new Guid("257f12b5-a5d7-4205-8d43-4d1842f48d7d"), "Approve, request corrections on, or reject a submitted tenant registration (owner stage)", "occupancy-registration", true, "occupancy-registration.owner-review" },
                { new Guid("e53aa1a7-e900-428a-af9e-85804b73dc20"), "Verify, request corrections on, reject, or activate an owner-approved tenant registration (management stage)", "occupancy-registration", true, "occupancy-registration.verify" },
                { new Guid("24095b1b-26c3-46d7-8049-398a75f6ff2f"), "Record the move-out of an active tenant registration", "occupancy-registration", true, "occupancy-registration.move-out" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'occupancy-registration.view', 'occupancy-registration.create',
                'occupancy-registration.owner-review', 'occupancy-registration.verify',
                'occupancy-registration.move-out'
            );
            """);
    }
}

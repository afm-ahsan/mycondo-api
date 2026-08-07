using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 15 permissions Slice G (Facilities — Community Hall Booking + Swimming Pool Management)
/// needs — same rationale as the prior Seed_*_Permissions migrations: these resources didn't exist in
/// the original catalogue. facility.booking.refund covers the inspect/settle-deposit action;
/// pool.override is separate since it's a materially more sensitive action (mirrors
/// parcel.escalate's/utility.reading.correct's precedent).
/// </summary>
public partial class Seed_Facility_Pool_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("76a5b6fb-0f69-4694-b428-908615e4f8c2"), "View facilities", "facility", true, "facility.view" },
                { new Guid("21335114-747b-4de8-bf92-0a13ff83ad25"), "Create and configure facilities and blackout dates", "facility", true, "facility.manage" },
                { new Guid("a0a4cb10-0345-4e04-8d84-64f7a2b83a41"), "View facility bookings", "facility", true, "facility.booking.view" },
                { new Guid("4c28757b-6c0e-4525-b574-88a579c44b55"), "Request a facility booking", "facility", true, "facility.booking.create" },
                { new Guid("3096fe86-4a37-44eb-9462-bcebedb426be"), "Approve or reject a pending facility booking", "facility", true, "facility.booking.approve" },
                { new Guid("a99986a1-77e2-4fea-b7b0-36d4b6488c1c"), "Cancel a facility booking or mark it no-show", "facility", true, "facility.booking.cancel" },
                { new Guid("c0116998-950b-48fc-a80e-337c2dbbdc51"), "Settle a facility booking's deposit after inspection", "facility", true, "facility.booking.refund" },
                { new Guid("269be3f5-ddf9-4b87-9477-3d99ec4fa96c"), "Check in, complete, and inspect a facility booking", "facility", true, "facility.booking.inspect" },
                { new Guid("8c411275-6ab6-41b4-9f03-c07bc337d5af"), "View swimming pool sessions and incidents", "pool", true, "pool.view" },
                { new Guid("ff31e024-5ab0-4952-a594-3dd870754f3f"), "Configure swimming pool facility settings", "pool", true, "pool.manage" },
                { new Guid("9f682b71-82fe-4897-b43c-ad6bf2f7cb12"), "Check a person into the swimming pool", "pool", true, "pool.checkin" },
                { new Guid("a4e0512c-4119-4a19-8747-c39f6ebb62af"), "Check a person out of the swimming pool", "pool", true, "pool.checkout" },
                { new Guid("71b963a2-9c47-4291-afe2-9469e9f84edc"), "Bypass swimming pool capacity or eligibility rules with an override reason", "pool", true, "pool.override" },
                { new Guid("7d42f3a8-44eb-42b9-8915-cd0435eaa7a3"), "Record a swimming pool incident", "pool", true, "pool.incident.manage" },
                { new Guid("fc72cc94-fa71-4f37-a694-12beec5fadac"), "View facility utilization and booking revenue reports", "report", false, "report.facility" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'facility.view', 'facility.manage',
                'facility.booking.view', 'facility.booking.create', 'facility.booking.approve',
                'facility.booking.cancel', 'facility.booking.refund', 'facility.booking.inspect',
                'pool.view', 'pool.manage', 'pool.checkin', 'pool.checkout', 'pool.override',
                'pool.incident.manage',
                'report.facility'
            );
            """);
    }
}

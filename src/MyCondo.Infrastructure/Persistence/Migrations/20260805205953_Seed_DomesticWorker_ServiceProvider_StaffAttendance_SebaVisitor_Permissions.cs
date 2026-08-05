using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 19 permissions Slice B2 (Domestic Worker, Service Provider, Staff Attendance, Seba
/// Visitor) needs — same rationale as Seed_Security_Permissions (Slice B): these resources didn't
/// exist in the original 47-permission catalogue. Follows the same flat
/// `&lt;resource&gt;[.&lt;subresource&gt;].&lt;action&gt;` naming convention.
/// </summary>
public partial class Seed_DomesticWorker_ServiceProvider_StaffAttendance_SebaVisitor_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("53a9dbae-667a-4bfe-a38d-34020d4548fa"), "View domestic worker profiles and assignments", "domesticworker", true, "domesticworker.view" },
                { new Guid("7384fb61-5b02-4712-98db-4a47bc10dfd2"), "Register and manage domestic worker profiles (status, verification)", "domesticworker", true, "domesticworker.manage" },
                { new Guid("5367cfdc-1b4d-4255-9a51-11b297884da7"), "Create, approve, and deactivate domestic worker flat assignments", "domesticworker", true, "domesticworker.assignment.manage" },
                { new Guid("69794a81-ffb9-497e-a878-d092abae2999"), "Check domestic workers in", "domesticworker", true, "domesticworker.checkin" },
                { new Guid("bd411aec-a3fb-47de-ac78-cfd7d0f31dc6"), "Check domestic workers out", "domesticworker", true, "domesticworker.checkout" },
                { new Guid("861c3ef6-d6d1-4d68-97f0-e41d0b8f4b59"), "Override a blocked/suspended/out-of-schedule domestic worker's entry", "domesticworker", true, "domesticworker.override" },
                { new Guid("e08a418b-2c0f-4c32-9803-f75b6a11de09"), "View service provider profiles and assignments", "serviceprovider", true, "serviceprovider.view" },
                { new Guid("8ed52776-8af5-4425-984f-0cea92c8d031"), "Register and manage service provider profiles (status, verification)", "serviceprovider", true, "serviceprovider.manage" },
                { new Guid("2707a9af-2b0b-419f-9f21-befeff916b7d"), "Create, approve, and deactivate service provider flat assignments", "serviceprovider", true, "serviceprovider.assignment.manage" },
                { new Guid("b2ab0808-365c-4449-835d-719c976a19f6"), "Check service providers in", "serviceprovider", true, "serviceprovider.checkin" },
                { new Guid("c5e32a05-052d-4160-91f3-34aca0f38531"), "Check service providers out", "serviceprovider", true, "serviceprovider.checkout" },
                { new Guid("df113cf4-75e2-4c5e-8bb1-41493a32e5a0"), "Override a blocked/suspended/out-of-schedule service provider's entry", "serviceprovider", true, "serviceprovider.override" },
                { new Guid("17f4a3f1-2961-425a-8a49-5451356b20ad"), "View staff members and attendance records", "staffattendance", true, "staffattendance.view" },
                { new Guid("728e6f36-d7c0-49b6-a3fa-9a7a00649607"), "Register staff members and record clock-in/clock-out", "staffattendance", true, "staffattendance.manage" },
                { new Guid("c29ae28b-dcb2-43d9-bf74-06b13aa9d8c6"), "Request a correction to an attendance record", "staffattendance", true, "staffattendance.correct" },
                { new Guid("ee48c2cc-57d8-430d-9e27-ab876942ea6a"), "Approve a requested attendance correction", "staffattendance", true, "staffattendance.approve" },
                { new Guid("f5ff7170-227f-4893-b6f8-9bb56b400deb"), "View Seba office visits", "sebavisitor", true, "sebavisitor.view" },
                { new Guid("3ee03455-bd3c-4050-a796-9d20ca9209f0"), "Check Seba office visitors in", "sebavisitor", true, "sebavisitor.manage" },
                { new Guid("a8701736-3c8f-4a77-a98d-eab05f551ca8"), "Check Seba office visitors out and record outcome", "sebavisitor", true, "sebavisitor.checkout" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'domesticworker.view', 'domesticworker.manage', 'domesticworker.assignment.manage',
                'domesticworker.checkin', 'domesticworker.checkout', 'domesticworker.override',
                'serviceprovider.view', 'serviceprovider.manage', 'serviceprovider.assignment.manage',
                'serviceprovider.checkin', 'serviceprovider.checkout', 'serviceprovider.override',
                'staffattendance.view', 'staffattendance.manage', 'staffattendance.correct', 'staffattendance.approve',
                'sebavisitor.view', 'sebavisitor.manage', 'sebavisitor.checkout'
            );
            """);
    }
}

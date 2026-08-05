using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 7 permissions Slice C (Parcel register) needs — same rationale as the prior
/// Seed_Security_Permissions migrations: this resource didn't exist in the original catalogue.
/// parcel.return covers both Returned and Rejected outcomes of CloseParcelCommand; the
/// LostOrEscalated outcome additionally requires parcel.escalate (checked in the handler, since one
/// command covers all three terminal-close outcomes).
/// </summary>
public partial class Seed_Parcel_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("d3116566-383b-423f-bb5e-eb9122895c21"), "View parcels and custody history", "parcel", true, "parcel.view" },
                { new Guid("41fd0b71-c932-4882-8585-9d9e1586bf47"), "Receive parcels at the front desk", "parcel", true, "parcel.receive" },
                { new Guid("691923b7-9927-4556-9108-cb2ff093e629"), "Update parcel details (e.g. mark damaged)", "parcel", true, "parcel.update" },
                { new Guid("e4f0b293-b171-48dd-a12c-c1053e160c6d"), "Notify a resident their parcel has arrived", "parcel", true, "parcel.notify" },
                { new Guid("346c1170-6ae9-426e-83ab-f341841ac7e2"), "Hand a parcel over to its collector", "parcel", true, "parcel.handover" },
                { new Guid("4d261975-9c53-49bb-b922-84318d044839"), "Return or reject a parcel", "parcel", true, "parcel.return" },
                { new Guid("4314bb1e-5774-4084-89fa-796dc3fa6496"), "Escalate a parcel as lost", "parcel", true, "parcel.escalate" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'parcel.view', 'parcel.receive', 'parcel.update', 'parcel.notify',
                'parcel.handover', 'parcel.return', 'parcel.escalate'
            );
            """);
    }
}

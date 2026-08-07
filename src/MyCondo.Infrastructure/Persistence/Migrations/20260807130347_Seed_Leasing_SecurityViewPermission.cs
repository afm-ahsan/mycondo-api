using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the security-facing read permission for Tenant Registration — a deliberately restricted view
/// (name/photo/unit/occupancy status/approved household/workers/vehicles only, never NID/passport/
/// address/lease data), separate from <c>occupancy-registration.view</c> so security staff can be
/// granted this narrower permission instead of the full management view.
/// </summary>
public partial class Seed_Leasing_SecurityViewPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("6d64b81c-1e05-4af5-afdc-705daff9c8ad"), "View the restricted security-facing tenant registration directory (operational info only, no sensitive personal data)", "occupancy-registration", true, "occupancy-registration.security-view" },
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions WHERE name = 'occupancy-registration.security-view';
            """);
    }
}

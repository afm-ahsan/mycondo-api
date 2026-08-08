using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the permission for UX-5's 3 new tenant-wide Utilities aggregate reports (consumption
/// summary, reading status summary, meter status summary) — module "report", not "utility",
/// matching the report.facility/report.financial.view/report.operational.view precedent (report
/// permissions are not building-scopable, unlike the underlying report data's own optional
/// buildingId filter).
/// </summary>
public partial class Seed_UtilityReport_Permission : Migration
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
                { new Guid("d5cb563e-7030-4d90-89f7-69f9d7567603"), "View utility consumption, reading-status, and meter-status reports", "report", false, "utility.report" },
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions WHERE name = 'utility.report';
            """);
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 8 permissions Slice F (Electricity &amp; Gas Billing) needs — same rationale as the
/// prior Seed_*_Permissions migrations: these resources didn't exist in the original catalogue.
/// utility.reading.record covers Draft entry + Review; utility.reading.finalize covers Finalize +
/// Bill; utility.reading.correct is separate since it's a materially more sensitive action (mirrors
/// parcel.escalate's precedent).
/// </summary>
public partial class Seed_Utility_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("aa2e6ff3-8704-4ba7-8d50-71683439c4ae"), "View electricity/gas meters", "utility", true, "utility.meter.view" },
                { new Guid("96361ac1-a85b-4993-a3ba-22206a26a77e"), "Install, assign, and manage meters", "utility", true, "utility.meter.manage" },
                { new Guid("53ece993-d53c-4a38-b69c-31e153c4107a"), "View utility rate plans", "utility", true, "utility.rateplan.view" },
                { new Guid("8e2b4360-757c-4471-a393-0cc2fd1230df"), "Create and end utility rate plans", "utility", true, "utility.rateplan.manage" },
                { new Guid("024556b7-b151-4353-99ef-c5a04c549e57"), "View meter readings and consumption history", "utility", true, "utility.reading.view" },
                { new Guid("a0a89292-076a-413e-8219-a1646e3fe767"), "Record and review meter readings", "utility", true, "utility.reading.record" },
                { new Guid("f4aa6bc8-fb56-4d41-92a8-becdd60b26a2"), "Finalize and bill a meter reading", "utility", true, "utility.reading.finalize" },
                { new Guid("5b052e93-8a06-4a13-87b3-10e48d31686b"), "Correct a finalized or billed meter reading", "utility", true, "utility.reading.correct" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'utility.meter.view', 'utility.meter.manage',
                'utility.rateplan.view', 'utility.rateplan.manage',
                'utility.reading.view', 'utility.reading.record', 'utility.reading.finalize',
                'utility.reading.correct'
            );
            """);
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the 5 permissions Slice D (Financial Foundation) needs — same rationale as the prior
/// Seed_*_Permissions migrations: these resources didn't exist in the original catalogue.
/// residentaccount.manage covers opening accounts and recording opening balances; payment.record and
/// payment.reverse are separate since reversal is a materially more sensitive action.
/// </summary>
public partial class Seed_Payments_Permissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new Guid("a29b5929-fcc6-4f17-afc2-c394cd23f947"), "View resident payments and account balance", "payment", true, "payment.view" },
                { new Guid("c8c551f6-2e15-4d82-b55f-70f86b4c8b5c"), "Record a resident payment", "payment", true, "payment.record" },
                { new Guid("931cb647-e8ef-46b2-8426-6473828b0224"), "Reverse a posted resident payment", "payment", true, "payment.reverse" },
                { new Guid("537f62c4-bdbb-4024-830a-1aea7afb5a03"), "View resident account details and ledger history", "residentaccount", true, "residentaccount.view" },
                { new Guid("d4d9d017-cb69-404e-a3d5-30e4fbc577fd"), "Open resident accounts and record opening balances", "residentaccount", true, "residentaccount.manage" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM identity.permissions
            WHERE name IN (
                'payment.view', 'payment.record', 'payment.reverse',
                'residentaccount.view', 'residentaccount.manage'
            );
            """);
    }
}

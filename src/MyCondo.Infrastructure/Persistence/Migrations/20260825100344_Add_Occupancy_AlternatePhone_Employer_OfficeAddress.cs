using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Occupancy_AlternatePhone_Employer_OfficeAddress : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "primary_alternate_phone",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_employer",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_office_address",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(400)",
            maxLength: 400,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "primary_alternate_phone",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_employer",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_office_address",
            schema: "leasing",
            table: "occupancy_registrations");
    }
}

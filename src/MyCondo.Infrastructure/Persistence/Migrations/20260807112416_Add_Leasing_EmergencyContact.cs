using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Leasing_EmergencyContact : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "emergency_contact_name",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "emergency_contact_phone",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "emergency_contact_name",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "emergency_contact_phone",
            schema: "leasing",
            table: "occupancy_registrations");
    }
}

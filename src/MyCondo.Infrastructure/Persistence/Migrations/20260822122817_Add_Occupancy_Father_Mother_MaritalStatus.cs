using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Occupancy_Father_Mother_MaritalStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "primary_father_name",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_marital_status",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_mother_name",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "primary_father_name",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_marital_status",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_mother_name",
            schema: "leasing",
            table: "occupancy_registrations");
    }
}

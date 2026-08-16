using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Owner_Tenant_Household_Demographics : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "blood_group",
            schema: "residents",
            table: "residents",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "nationality",
            schema: "residents",
            table: "residents",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "religion",
            schema: "residents",
            table: "residents",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_blood_group",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_gender",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_nationality",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_profession",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_religion",
            schema: "leasing",
            table: "occupancy_registrations",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "birth_certificate_number",
            schema: "leasing",
            table: "household_members",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "blood_group",
            schema: "leasing",
            table: "household_members",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "gender",
            schema: "leasing",
            table: "household_members",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "nationality",
            schema: "leasing",
            table: "household_members",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "occupation",
            schema: "leasing",
            table: "household_members",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "religion",
            schema: "leasing",
            table: "household_members",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "household_members",
            schema: "residents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                relationship_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                national_id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                birth_certificate_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                blood_group = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                religion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                nationality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                occupation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_resident_household_members", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_resident_household_members_tenant_id_resident_id",
            schema: "residents",
            table: "household_members",
            columns: new[] { "tenant_id", "resident_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "household_members",
            schema: "residents");

        migrationBuilder.DropColumn(
            name: "blood_group",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "nationality",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "religion",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "primary_blood_group",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_gender",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_nationality",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_profession",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "primary_religion",
            schema: "leasing",
            table: "occupancy_registrations");

        migrationBuilder.DropColumn(
            name: "birth_certificate_number",
            schema: "leasing",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "blood_group",
            schema: "leasing",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "gender",
            schema: "leasing",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "nationality",
            schema: "leasing",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "occupation",
            schema: "leasing",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "religion",
            schema: "leasing",
            table: "household_members");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_HouseholdMember_PrimaryPhoto : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "primary_photo_attachment_id",
            schema: "residents",
            table: "household_members",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "primary_photo_attachment_id",
            schema: "leasing",
            table: "household_members",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "primary_photo_attachment_id",
            schema: "residents",
            table: "household_members");

        migrationBuilder.DropColumn(
            name: "primary_photo_attachment_id",
            schema: "leasing",
            table: "household_members");
    }
}

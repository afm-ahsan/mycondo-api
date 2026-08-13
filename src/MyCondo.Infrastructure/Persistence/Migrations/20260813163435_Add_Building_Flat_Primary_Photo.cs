using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds a nullable <c>primary_photo_attachment_id</c> column to <c>property.buildings</c> and
/// <c>property.flats</c>, mirroring <c>occupancy_registrations.primary_photo_attachment_id</c> — a
/// loose FK-by-value into <c>documents.attachments</c> (no DB-level FK; validated in the command
/// handler like the occupancy-registration primary photo). No RLS changes: both tables already
/// carry tenant-isolation policies from prior migrations.
/// </summary>
public partial class Add_Building_Flat_Primary_Photo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "primary_photo_attachment_id",
            schema: "property",
            table: "flats",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "primary_photo_attachment_id",
            schema: "property",
            table: "buildings",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "primary_photo_attachment_id",
            schema: "property",
            table: "flats");

        migrationBuilder.DropColumn(
            name: "primary_photo_attachment_id",
            schema: "property",
            table: "buildings");
    }
}

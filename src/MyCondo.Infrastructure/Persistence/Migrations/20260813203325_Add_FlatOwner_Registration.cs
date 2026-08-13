using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Flat Owner Registration. Two changes:
/// (1) property.flat_ownerships.user_id is renamed to resident_id — FlatOwnership now references
///     the shared Resident party record instead of a portal User, so an owner's profile can be
///     recorded without requiring a User/login to exist first (see FlatOwnership's doc comment).
///     This is a straight column rename, not a data migration: no seeder writes flat_ownerships
///     rows, but any row created manually in dev before this migration will carry a stale User ID
///     under the new resident_id column — harmless (it just won't resolve to a Resident and the
///     row becomes invisible in the register) but not a real ownership record; pre-launch only.
/// (2) residents.residents gains the owner-profile columns captured by the registration wizard
///     (identity, contact, family/professional) — nullable, unused by Occupant/FamilyMember rows.
/// </summary>
public partial class Add_FlatOwner_Registration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "user_id",
            schema: "property",
            table: "flat_ownerships",
            newName: "resident_id");

        migrationBuilder.RenameIndex(
            name: "ux_flat_ownerships_tenant_id_user_id_flat_id_active",
            schema: "property",
            table: "flat_ownerships",
            newName: "ux_flat_ownerships_tenant_id_resident_id_flat_id_active");

        migrationBuilder.RenameIndex(
            name: "ix_flat_ownerships_tenant_id_user_id",
            schema: "property",
            table: "flat_ownerships",
            newName: "ix_flat_ownerships_tenant_id_resident_id");

        migrationBuilder.AddColumn<string>(
            name: "alternate_phone",
            schema: "residents",
            table: "residents",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "date_of_birth",
            schema: "residents",
            table: "residents",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "emergency_contact_name",
            schema: "residents",
            table: "residents",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "emergency_contact_phone",
            schema: "residents",
            table: "residents",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "employer",
            schema: "residents",
            table: "residents",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "father_name",
            schema: "residents",
            table: "residents",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "gender",
            schema: "residents",
            table: "residents",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "marital_status",
            schema: "residents",
            table: "residents",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "mother_name",
            schema: "residents",
            table: "residents",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "national_id_number",
            schema: "residents",
            table: "residents",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "office_address",
            schema: "residents",
            table: "residents",
            type: "character varying(400)",
            maxLength: 400,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "passport_number",
            schema: "residents",
            table: "residents",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "permanent_address",
            schema: "residents",
            table: "residents",
            type: "character varying(400)",
            maxLength: 400,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "present_address",
            schema: "residents",
            table: "residents",
            type: "character varying(400)",
            maxLength: 400,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "profession",
            schema: "residents",
            table: "residents",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "alternate_phone",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "date_of_birth",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "emergency_contact_name",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "emergency_contact_phone",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "employer",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "father_name",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "gender",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "marital_status",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "mother_name",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "national_id_number",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "office_address",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "passport_number",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "permanent_address",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "present_address",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "profession",
            schema: "residents",
            table: "residents");

        migrationBuilder.RenameColumn(
            name: "resident_id",
            schema: "property",
            table: "flat_ownerships",
            newName: "user_id");

        migrationBuilder.RenameIndex(
            name: "ux_flat_ownerships_tenant_id_resident_id_flat_id_active",
            schema: "property",
            table: "flat_ownerships",
            newName: "ux_flat_ownerships_tenant_id_user_id_flat_id_active");

        migrationBuilder.RenameIndex(
            name: "ix_flat_ownerships_tenant_id_resident_id",
            schema: "property",
            table: "flat_ownerships",
            newName: "ix_flat_ownerships_tenant_id_user_id");
    }
}

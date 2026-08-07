using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Leasing_OccupancyRegistrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "leasing");

        migrationBuilder.CreateTable(
            name: "household_members",
            schema: "leasing",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                occupancy_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                relationship_to_primary = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                national_id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_household_members", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "occupancy_registration_status_histories",
            schema: "leasing",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                occupancy_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                from_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                to_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_occupancy_registration_status_histories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "occupancy_registrations",
            schema: "leasing",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                primary_resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                occupancy_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                primary_full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                primary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                primary_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                primary_national_id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                primary_date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                primary_permanent_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                primary_photo_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                move_in_expected_date = table.Column<DateOnly>(type: "date", nullable: true),
                status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                submitted_by = table.Column<Guid>(type: "uuid", nullable: true),
                owner_reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                owner_reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                management_verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                management_verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                moved_out_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                move_out_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                corrections_requested_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_occupancy_registrations", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_household_members_tenant_id_occupancy_registration_id",
            schema: "leasing",
            table: "household_members",
            columns: new[] { "tenant_id", "occupancy_registration_id" });

        migrationBuilder.CreateIndex(
            name: "ix_occ_reg_status_histories_tenant_id_occ_reg_id_changed_at_utc",
            schema: "leasing",
            table: "occupancy_registration_status_histories",
            columns: new[] { "tenant_id", "occupancy_registration_id", "changed_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_occupancy_registrations_tenant_id_flat_id_status",
            schema: "leasing",
            table: "occupancy_registrations",
            columns: new[] { "tenant_id", "flat_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "household_members",
            schema: "leasing");

        migrationBuilder.DropTable(
            name: "occupancy_registration_status_histories",
            schema: "leasing");

        migrationBuilder.DropTable(
            name: "occupancy_registrations",
            schema: "leasing");
    }
}

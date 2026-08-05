using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Security_Guests_Vehicles_AccessSessions : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.CreateTable(
                name: "access_sessions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guest_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    host_flat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose_of_visit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    entry_gate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    exit_gate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exit_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_in_by = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_out_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pass_or_qr_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    override_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guest_profiles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    identity_document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    identity_document_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    vehicle_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    make = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ownership_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_sessions_tenant_id_category_status",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "access_category", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_guest_profile_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "guest_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND guest_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_vehicle_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "vehicle_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND vehicle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_guest_profiles_tenant_id_phone",
                schema: "security",
                table: "guest_profiles",
                columns: new[] { "tenant_id", "phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_tenant_id_flat_id",
                schema: "security",
                table: "vehicles",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ux_vehicles_tenant_id_registration_number",
                schema: "security",
                table: "vehicles",
                columns: new[] { "tenant_id", "registration_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_sessions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "guest_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "security");
        }
    }

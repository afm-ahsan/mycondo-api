using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Security_DomesticWorkers_ServiceProviders_SebaVisits : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.AddColumn<Guid>(
                name: "domestic_worker_profile_id",
                schema: "security",
                table: "access_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "service_provider_profile_id",
                schema: "security",
                table: "access_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheduled_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_in_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    check_out_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    work_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correction_requested = table.Column<bool>(type: "boolean", nullable: false),
                    correction_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domestic_worker_assignments",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domestic_worker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_resident = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allowed_days = table.Column<int>(type: "integer", nullable: false),
                    allowed_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    allowed_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domestic_worker_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domestic_worker_profiles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    worker_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    identity_document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    identity_document_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("pk_domestic_worker_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seba_visit_details",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visitor_full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    visitor_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department_or_employee_to_meet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    token_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    related_reference_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    related_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_outcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seba_visit_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_provider_assignments",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_provider_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_resident = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allowed_days = table.Column<int>(type: "integer", nullable: false),
                    allowed_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    allowed_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_provider_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_provider_profiles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    service_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    identity_document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    identity_document_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("pk_service_provider_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_members",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_staff_members", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_domestic_worker_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "domestic_worker_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND domestic_worker_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_service_provider_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "service_provider_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND service_provider_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_tenant_id_staff_member_id_work_date",
                schema: "payroll",
                table: "attendance_records",
                columns: new[] { "tenant_id", "staff_member_id", "work_date" });

            migrationBuilder.CreateIndex(
                name: "ux_attendance_records_tenant_id_staff_member_id_open",
                schema: "payroll",
                table: "attendance_records",
                columns: new[] { "tenant_id", "staff_member_id" },
                unique: true,
                filter: "check_out_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_domestic_worker_assignments_tenant_id_flat_id",
                schema: "security",
                table: "domestic_worker_assignments",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_domestic_worker_assignments_tenant_id_worker_id",
                schema: "security",
                table: "domestic_worker_assignments",
                columns: new[] { "tenant_id", "domestic_worker_profile_id" });

            migrationBuilder.CreateIndex(
                name: "ix_domestic_worker_profiles_tenant_id_phone",
                schema: "security",
                table: "domestic_worker_profiles",
                columns: new[] { "tenant_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ux_seba_visit_details_access_session_id",
                schema: "security",
                table: "seba_visit_details",
                column: "access_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_provider_assignments_tenant_id_flat_id",
                schema: "security",
                table: "service_provider_assignments",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_provider_assignments_tenant_id_provider_id",
                schema: "security",
                table: "service_provider_assignments",
                columns: new[] { "tenant_id", "service_provider_profile_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_provider_profiles_tenant_id_phone",
                schema: "security",
                table: "service_provider_profiles",
                columns: new[] { "tenant_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_members_tenant_id_full_name",
                schema: "payroll",
                table: "staff_members",
                columns: new[] { "tenant_id", "full_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "domestic_worker_assignments",
                schema: "security");

            migrationBuilder.DropTable(
                name: "domestic_worker_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "seba_visit_details",
                schema: "security");

            migrationBuilder.DropTable(
                name: "service_provider_assignments",
                schema: "security");

            migrationBuilder.DropTable(
                name: "service_provider_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "staff_members",
                schema: "payroll");

            migrationBuilder.DropIndex(
                name: "ux_access_sessions_tenant_id_domestic_worker_id_open",
                schema: "security",
                table: "access_sessions");

            migrationBuilder.DropIndex(
                name: "ux_access_sessions_tenant_id_service_provider_id_open",
                schema: "security",
                table: "access_sessions");

            migrationBuilder.DropColumn(
                name: "domestic_worker_profile_id",
                schema: "security",
                table: "access_sessions");

            migrationBuilder.DropColumn(
                name: "service_provider_profile_id",
                schema: "security",
                table: "access_sessions");
        }
    }

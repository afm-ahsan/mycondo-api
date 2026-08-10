using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class InitialPlatformSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.EnsureSchema(
                name: "amenities");

            migrationBuilder.EnsureSchema(
                name: "property");

            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.EnsureSchema(
                name: "leasing");

            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "utilities");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.EnsureSchema(
                name: "residents");

            migrationBuilder.EnsureSchema(
                name: "tenancy");

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
                    domestic_worker_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_provider_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "attachments",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                });

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
                name: "blackout_dates",
                schema: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blackout_dates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    setup_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    cleanup_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    expected_guest_count = table.Column<int>(type: "integer", nullable: false),
                    booking_charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cancellation_deadline_hours = table.Column<int>(type: "integer", nullable: false),
                    cancellation_deduction_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    approval_required = table.Column<bool>(type: "boolean", nullable: false),
                    payment_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deposit_collection_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deposit_settlement_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deposit_refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    deposit_deducted_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    terms_accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_in_by = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_in_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspection_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    damage_deduction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buildings",
                schema: "property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
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
                    table.PrimaryKey("pk_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cylinder_purchases",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    cylinder_weight_kg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    rate_per_cylinder = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    delivery_or_other_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payment_status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cylinder_purchases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cylinder_stock_movements",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cylinder_purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cylinder_stock_movements", x => x.id);
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
                name: "facilities",
                schema: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    facility_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    operating_hours_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    operating_hours_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    booking_charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    cancellation_deadline_hours = table.Column<int>(type: "integer", nullable: false),
                    cancellation_deduction_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    guest_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    minimum_age_unaccompanied = table.Column<int>(type: "integer", nullable: true),
                    requires_safety_acknowledgement = table.Column<bool>(type: "boolean", nullable: false),
                    blocks_entry_if_account_overdue = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facilities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "flat_ownerships",
                schema: "property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flat_ownerships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "flats",
                schema: "property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    floor_number = table.Column<int>(type: "integer", nullable: true),
                    flat_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    area_sq_ft = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
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
                    table.PrimaryKey("pk_flats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gas_cylinder_suppliers",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gas_cylinder_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gates",
                schema: "property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generator_breakdown_records",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    downtime_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    downtime_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generator_breakdown_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generator_fuel_receipts",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generator_fuel_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generator_maintenance_schedules",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_due_hour_meter_reading = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generator_maintenance_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generator_service_records",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generator_service_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generator_sessions",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    stop_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opening_fuel_level = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    closing_fuel_level = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    outage_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    runtime_minutes = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generator_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "generators",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    capacity_kva = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    current_hour_meter_reading = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generators", x => x.id);
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
                name: "idempotency_keys",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_charge_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rule_category_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    calculation_method_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rate_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    area_sq_ft_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ledger_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    void_ledger_posting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_postings",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_postings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meter_assignments",
                schema: "utilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meter_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meters",
                schema: "utilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    meter_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    replaces_meter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_cylinder_reconciliations",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cylinder_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_month = table.Column<DateOnly>(type: "date", nullable: false),
                    opening_stock = table.Column<int>(type: "integer", nullable: false),
                    total_received = table.Column<int>(type: "integer", nullable: false),
                    total_issued = table.Column<int>(type: "integer", nullable: false),
                    total_empty_returned = table.Column<int>(type: "integer", nullable: false),
                    closing_stock = table.Column<int>(type: "integer", nullable: false),
                    variance_quantity = table.Column<int>(type: "integer", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reconciled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reconciled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monthly_cylinder_reconciliations", x => x.id);
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
                name: "occupancy_registration_vehicle_assignments",
                schema: "leasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occupancy_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_occupancy_registration_vehicle_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "occupancy_registration_worker_assignments",
                schema: "leasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occupancy_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domestic_worker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_occupancy_registration_worker_assignments", x => x.id);
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
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "parcel_custody_events",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parcel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parcel_custody_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parcels",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parcel_reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    courier_provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    sender_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recipient_flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_resident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parcel_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    package_count = table.Column<int>(type: "integer", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    collected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    collected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    collector_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    collection_acknowledgement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    damage_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    close_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parcels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ledger_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_building_scopable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_audit_log",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    target_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_refresh_tokens",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_role_permissions",
                schema: "platform",
                columns: table => new
                {
                    platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_role_permissions", x => new { x.platform_role_id, x.permission_id });
                });

            migrationBuilder.CreateTable(
                name: "platform_roles",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_user_role_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_user_role_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_users",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pool_incidents",
                schema: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pool_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reported_by = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action_taken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pool_incidents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pool_sessions",
                schema: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    age_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accompanied_by_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    exit_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    guest_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    safety_acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_in_by = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_out_by = table.Column<Guid>(type: "uuid", nullable: true),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pool_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rate_plans",
                schema: "utilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    structure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    fixed_service_charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rate_slabs",
                schema: "utilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slab_order = table.Column<int>(type: "integer", nullable: false),
                    from_units = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    to_units = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    rate_per_unit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_slabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "readings",
                schema: "utilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    previous_reading = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    present_reading = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    consumption_units = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_abnormal_consumption = table.Column<bool>(type: "boolean", nullable: false),
                    abnormal_consumption_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    billed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    billed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corrects_reading_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_readings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resident_accounts",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resident_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "residents",
                schema: "residents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    resident_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_residents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_assignments",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    requires_building_scope = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("pk_roles", x => x.id);
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
                name: "service_charge_rules",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_type_filter = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_charge_rules", x => x.id);
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

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "tenancy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_users", x => x.id);
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
                name: "ux_access_sessions_tenant_id_domestic_worker_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "domestic_worker_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND domestic_worker_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_guest_profile_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "guest_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND guest_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_service_provider_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "service_provider_profile_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND service_provider_profile_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_access_sessions_tenant_id_vehicle_id_open",
                schema: "security",
                table: "access_sessions",
                columns: new[] { "tenant_id", "vehicle_id" },
                unique: true,
                filter: "status = 'CheckedIn' AND vehicle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_tenant_id_owner_type_owner_id",
                schema: "documents",
                table: "attachments",
                columns: new[] { "tenant_id", "owner_type", "owner_id" });

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
                name: "ix_blackout_dates_tenant_id_facility_id_is_active",
                schema: "amenities",
                table: "blackout_dates",
                columns: new[] { "tenant_id", "facility_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_tenant_id_facility_id_start_at_utc",
                schema: "amenities",
                table: "bookings",
                columns: new[] { "tenant_id", "facility_id", "start_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_tenant_id_flat_id_status",
                schema: "amenities",
                table: "bookings",
                columns: new[] { "tenant_id", "flat_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_buildings_tenant_id_code",
                schema: "property",
                table: "buildings",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_buildings_tenant_id_name",
                schema: "property",
                table: "buildings",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cylinder_purchases_tenant_id_supplier_id_purchase_date",
                schema: "operations",
                table: "cylinder_purchases",
                columns: new[] { "tenant_id", "supplier_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_cylinder_stock_movements_tenant_id_cylinder_type_occurred_at_utc",
                schema: "operations",
                table: "cylinder_stock_movements",
                columns: new[] { "tenant_id", "cylinder_type", "occurred_at_utc" });

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
                name: "ix_facilities_tenant_id_building_id_facility_type",
                schema: "amenities",
                table: "facilities",
                columns: new[] { "tenant_id", "building_id", "facility_type" });

            migrationBuilder.CreateIndex(
                name: "ix_flat_ownerships_tenant_id_flat_id",
                schema: "property",
                table: "flat_ownerships",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_flat_ownerships_tenant_id_user_id",
                schema: "property",
                table: "flat_ownerships",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_flat_ownerships_tenant_id_user_id_flat_id_active",
                schema: "property",
                table: "flat_ownerships",
                columns: new[] { "tenant_id", "user_id", "flat_id" },
                unique: true,
                filter: "\"status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_flats_tenant_id_building_id",
                schema: "property",
                table: "flats",
                columns: new[] { "tenant_id", "building_id" });

            migrationBuilder.CreateIndex(
                name: "ux_flats_tenant_id_building_id_flat_number",
                schema: "property",
                table: "flats",
                columns: new[] { "tenant_id", "building_id", "flat_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gas_cylinder_suppliers_tenant_id_is_active",
                schema: "operations",
                table: "gas_cylinder_suppliers",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_gates_tenant_id_building_id_name",
                schema: "property",
                table: "gates",
                columns: new[] { "tenant_id", "building_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_generator_breakdown_records_tenant_id_generator_id_reported_at_utc",
                schema: "operations",
                table: "generator_breakdown_records",
                columns: new[] { "tenant_id", "generator_id", "reported_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_generator_fuel_receipts_tenant_id_generator_id_received_at_utc",
                schema: "operations",
                table: "generator_fuel_receipts",
                columns: new[] { "tenant_id", "generator_id", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_generator_maintenance_schedules_tenant_id_generator_id_is_active",
                schema: "operations",
                table: "generator_maintenance_schedules",
                columns: new[] { "tenant_id", "generator_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_generator_service_records_tenant_id_generator_id_performed_at_utc",
                schema: "operations",
                table: "generator_service_records",
                columns: new[] { "tenant_id", "generator_id", "performed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_generator_sessions_tenant_id_generator_id_status",
                schema: "operations",
                table: "generator_sessions",
                columns: new[] { "tenant_id", "generator_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_generators_tenant_id_building_id",
                schema: "operations",
                table: "generators",
                columns: new[] { "tenant_id", "building_id" });

            migrationBuilder.CreateIndex(
                name: "ux_guest_profiles_tenant_id_phone",
                schema: "security",
                table: "guest_profiles",
                columns: new[] { "tenant_id", "phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_household_members_tenant_id_occupancy_registration_id",
                schema: "leasing",
                table: "household_members",
                columns: new[] { "tenant_id", "occupancy_registration_id" });

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_keys_tenant_id_key_request_path",
                schema: "payments",
                table: "idempotency_keys",
                columns: new[] { "tenant_id", "key", "request_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_tenant_id_invoice_id",
                schema: "billing",
                table: "invoice_lines",
                columns: new[] { "tenant_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ux_invoice_lines_tenant_id_invoice_id_rule_id",
                schema: "billing",
                table: "invoice_lines",
                columns: new[] { "tenant_id", "invoice_id", "service_charge_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id_building_id_status",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "building_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id_flat_id_status",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
                unique: true,
                filter: "source <> 'FacilityBooking'");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_invoice_number",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_tenant_id_flat_id_account_type",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "tenant_id", "flat_id", "account_type" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_tenant_id_posting_id",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "tenant_id", "posting_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_postings_tenant_id_business_date",
                schema: "payments",
                table: "ledger_postings",
                columns: new[] { "tenant_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_postings_tenant_id_reference_type_reference_id",
                schema: "payments",
                table: "ledger_postings",
                columns: new[] { "tenant_id", "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_meter_assignments_tenant_id_flat_id",
                schema: "utilities",
                table: "meter_assignments",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_meter_assignments_tenant_id_meter_id_assigned_from",
                schema: "utilities",
                table: "meter_assignments",
                columns: new[] { "tenant_id", "meter_id", "assigned_from_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_meter_assignments_tenant_id_meter_id_open",
                schema: "utilities",
                table: "meter_assignments",
                columns: new[] { "tenant_id", "meter_id" },
                unique: true,
                filter: "assigned_to_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_meters_tenant_id_building_id",
                schema: "utilities",
                table: "meters",
                columns: new[] { "tenant_id", "building_id" });

            migrationBuilder.CreateIndex(
                name: "ux_meters_tenant_id_utility_type_meter_number",
                schema: "utilities",
                table: "meters",
                columns: new[] { "tenant_id", "utility_type", "meter_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_monthly_cylinder_reconciliations_tenant_id_cylinder_type_period_month",
                schema: "operations",
                table: "monthly_cylinder_reconciliations",
                columns: new[] { "tenant_id", "cylinder_type", "period_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_occ_reg_status_histories_tenant_id_occ_reg_id_changed_at_utc",
                schema: "leasing",
                table: "occupancy_registration_status_histories",
                columns: new[] { "tenant_id", "occupancy_registration_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_occ_reg_vehicle_assignments_tenant_id_occ_reg_id",
                schema: "leasing",
                table: "occupancy_registration_vehicle_assignments",
                columns: new[] { "tenant_id", "occupancy_registration_id" });

            migrationBuilder.CreateIndex(
                name: "ix_occ_reg_worker_assignments_tenant_id_occ_reg_id",
                schema: "leasing",
                table: "occupancy_registration_worker_assignments",
                columns: new[] { "tenant_id", "occupancy_registration_id" });

            migrationBuilder.CreateIndex(
                name: "ix_occupancy_registrations_tenant_id_flat_id_status",
                schema: "leasing",
                table: "occupancy_registrations",
                columns: new[] { "tenant_id", "flat_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_parcel_custody_events_tenant_id_parcel_id",
                schema: "security",
                table: "parcel_custody_events",
                columns: new[] { "tenant_id", "parcel_id" });

            migrationBuilder.CreateIndex(
                name: "ix_parcels_tenant_id_recipient_flat_id",
                schema: "security",
                table: "parcels",
                columns: new[] { "tenant_id", "recipient_flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_parcels_tenant_id_status",
                schema: "security",
                table: "parcels",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_parcels_tenant_id_parcel_reference",
                schema: "security",
                table: "parcels",
                columns: new[] { "tenant_id", "parcel_reference" },
                unique: true,
                filter: "parcel_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_tenant_id_flat_id",
                schema: "payments",
                table: "payment_allocations",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_tenant_id_invoice_id",
                schema: "payments",
                table: "payment_allocations",
                columns: new[] { "tenant_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_tenant_id_payment_id",
                schema: "payments",
                table: "payment_allocations",
                columns: new[] { "tenant_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_flat_id",
                schema: "payments",
                table: "payments",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_status_business_date",
                schema: "payments",
                table: "payments",
                columns: new[] { "tenant_id", "status", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ux_payments_ledger_posting_id",
                schema: "payments",
                table: "payments",
                column: "ledger_posting_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_permissions_name",
                schema: "identity",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_log_actor_platform_user_id",
                schema: "platform",
                table: "platform_audit_log",
                column: "actor_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_log_occurred_at_utc",
                schema: "platform",
                table: "platform_audit_log",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_platform_refresh_tokens_platform_user_id",
                schema: "platform",
                table: "platform_refresh_tokens",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_refresh_tokens_token_hash",
                schema: "platform",
                table: "platform_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_role_permissions_permission_id",
                schema: "platform",
                table: "platform_role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_roles_name",
                schema: "platform",
                table: "platform_roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_platform_user_role_assignments_user_id_role_id",
                schema: "platform",
                table: "platform_user_role_assignments",
                columns: new[] { "platform_user_id", "platform_role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_platform_users_email",
                schema: "platform",
                table: "platform_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pool_incidents_tenant_id_facility_id_occurred_at_utc",
                schema: "amenities",
                table: "pool_incidents",
                columns: new[] { "tenant_id", "facility_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pool_sessions_tenant_id_facility_id_exit_at_utc",
                schema: "amenities",
                table: "pool_sessions",
                columns: new[] { "tenant_id", "facility_id", "exit_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pool_sessions_tenant_id_flat_id",
                schema: "amenities",
                table: "pool_sessions",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rate_plans_tenant_id_building_id_utility_type",
                schema: "utilities",
                table: "rate_plans",
                columns: new[] { "tenant_id", "building_id", "utility_type" });

            migrationBuilder.CreateIndex(
                name: "ux_rate_slabs_tenant_id_rate_plan_id_slab_order",
                schema: "utilities",
                table: "rate_slabs",
                columns: new[] { "tenant_id", "rate_plan_id", "slab_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_readings_tenant_id_building_id_utility_type_status",
                schema: "utilities",
                table: "readings",
                columns: new[] { "tenant_id", "building_id", "utility_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_readings_tenant_id_flat_id",
                schema: "utilities",
                table: "readings",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_readings_tenant_id_meter_id_status",
                schema: "utilities",
                table: "readings",
                columns: new[] { "tenant_id", "meter_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_readings_tenant_id_meter_id_period_active",
                schema: "utilities",
                table: "readings",
                columns: new[] { "tenant_id", "meter_id", "period_start", "period_end" },
                unique: true,
                filter: "status <> 'Corrected'");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_tenant_id_user_id",
                schema: "identity",
                table: "refresh_tokens",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_accounts_tenant_id_flat_id",
                schema: "payments",
                table: "resident_accounts",
                columns: new[] { "tenant_id", "flat_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_residents_tenant_id_flat_id",
                schema: "residents",
                table: "residents",
                columns: new[] { "tenant_id", "flat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_residents_tenant_id_user_id",
                schema: "residents",
                table: "residents",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_tenant_id_user_id",
                schema: "identity",
                table: "role_assignments",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_role_assignments_user_role_building",
                schema: "identity",
                table: "role_assignments",
                columns: new[] { "tenant_id", "user_id", "role_id", "building_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                schema: "identity",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_tenant_id",
                schema: "identity",
                table: "role_permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_roles_tenant_id_code",
                schema: "identity",
                table: "roles",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "\"code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_roles_tenant_id_name",
                schema: "identity",
                table: "roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_seba_visit_details_access_session_id",
                schema: "security",
                table: "seba_visit_details",
                column: "access_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_charge_rules_tenant_id_building_id",
                schema: "billing",
                table: "service_charge_rules",
                columns: new[] { "tenant_id", "building_id" });

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

            migrationBuilder.CreateIndex(
                name: "ux_tenants_slug",
                schema: "tenancy",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_tenant_id_email",
                schema: "identity",
                table: "users",
                columns: new[] { "tenant_id", "email" },
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

            // Forward-looking schemas approved by the schema-per-module architecture (ADR-004) but with
            // no tables yet — no entity currently creates a table in any of these, so EF's own model-diff
            // has no reason to emit EnsureSchema for them. Created explicitly here so the schema-per-module
            // boundary is visible immediately, matching the originally-approved schema set, rather than
            // silently appearing only once each module's first table migration ships.
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS expenses;
                CREATE SCHEMA IF NOT EXISTS vendors;
                CREATE SCHEMA IF NOT EXISTS complaints;
                CREATE SCHEMA IF NOT EXISTS maintenance;
                CREATE SCHEMA IF NOT EXISTS notifications;
                CREATE SCHEMA IF NOT EXISTS reporting;
                CREATE SCHEMA IF NOT EXISTS audit;
                """);

            // billing.invoice_sequences has no EF entity mapping (see IInvoiceSequenceRepository's doc
            // comment) — a plain per-tenant/building/year counter, hand-written since EF's model diff
            // never sees it. Carried forward verbatim from the historical
            // Add_SliceE_Property_Billing_Payments_Tables migration.
            migrationBuilder.Sql(
                """
                CREATE TABLE billing.invoice_sequences (
                    tenant_id uuid NOT NULL,
                    building_id uuid NOT NULL,
                    year integer NOT NULL,
                    next_value integer NOT NULL,
                    CONSTRAINT pk_invoice_sequences PRIMARY KEY (tenant_id, building_id, year)
                );
                """);

            // Authoritative overlap guards (ServiceChargeRule, RatePlan, Booking) — PostgreSQL
            // EXCLUDE USING gist constraints have no EF Core fluent-API representation, so these are
            // hand-written raw SQL, carried forward verbatim from the historical
            // Add_SliceE_Property_Billing_Payments_Tables / Add_Utilities_Meters_RatePlans_Readings /
            // Add_Amenities_Facilities_Bookings_PoolSessions migrations. See ServiceChargeRule's and
            // Booking's doc comments and IServiceChargeRuleRepository.HasOverlappingRuleAsync for the
            // application-layer pre-check that gives a friendly error before these constraints fire.
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE billing.service_charge_rules
                ADD CONSTRAINT ex_service_charge_rules_no_overlap
                EXCLUDE USING gist (
                    tenant_id WITH =,
                    building_id WITH =,
                    category WITH =,
                    COALESCE(unit_type_filter, '') WITH =,
                    frequency WITH =,
                    daterange(effective_from, effective_to, '[]') WITH &&
                );

                ALTER TABLE utilities.rate_plans
                ADD CONSTRAINT ex_rate_plans_no_overlap
                EXCLUDE USING gist (
                    tenant_id WITH =,
                    building_id WITH =,
                    utility_type WITH =,
                    daterange(effective_from, effective_to, '[]') WITH &&
                );

                CREATE OR REPLACE FUNCTION amenities.booking_slot_range(
                    start_at_utc timestamptz,
                    end_at_utc timestamptz,
                    setup_buffer_minutes integer,
                    cleanup_buffer_minutes integer
                ) RETURNS tstzrange
                LANGUAGE sql
                IMMUTABLE
                AS $$
                    SELECT tstzrange(
                        start_at_utc - (setup_buffer_minutes * INTERVAL '1 minute'),
                        end_at_utc + (cleanup_buffer_minutes * INTERVAL '1 minute'),
                        '[]'
                    );
                $$;

                ALTER TABLE amenities.bookings
                ADD CONSTRAINT ex_bookings_no_overlap
                EXCLUDE USING gist (
                    tenant_id WITH =,
                    facility_id WITH =,
                    amenities.booking_slot_range(start_at_utc, end_at_utc, setup_buffer_minutes, cleanup_buffer_minutes) WITH &&
                )
                WHERE (status NOT IN ('Cancelled', 'Rejected', 'NoShow'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE amenities.bookings DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;
                DROP FUNCTION IF EXISTS amenities.booking_slot_range(timestamptz, timestamptz, integer, integer);
                ALTER TABLE utilities.rate_plans DROP CONSTRAINT IF EXISTS ex_rate_plans_no_overlap;
                ALTER TABLE billing.service_charge_rules DROP CONSTRAINT IF EXISTS ex_service_charge_rules_no_overlap;
                DROP TABLE IF EXISTS billing.invoice_sequences;
                """);

            migrationBuilder.DropTable(
                name: "access_sessions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "blackout_dates",
                schema: "amenities");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "amenities");

            migrationBuilder.DropTable(
                name: "buildings",
                schema: "property");

            migrationBuilder.DropTable(
                name: "cylinder_purchases",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "cylinder_stock_movements",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "domestic_worker_assignments",
                schema: "security");

            migrationBuilder.DropTable(
                name: "domestic_worker_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "facilities",
                schema: "amenities");

            migrationBuilder.DropTable(
                name: "flat_ownerships",
                schema: "property");

            migrationBuilder.DropTable(
                name: "flats",
                schema: "property");

            migrationBuilder.DropTable(
                name: "gas_cylinder_suppliers",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "gates",
                schema: "property");

            migrationBuilder.DropTable(
                name: "generator_breakdown_records",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "generator_fuel_receipts",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "generator_maintenance_schedules",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "generator_service_records",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "generator_sessions",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "generators",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "guest_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "household_members",
                schema: "leasing");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "invoice_lines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "ledger_postings",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "meter_assignments",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "meters",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "monthly_cylinder_reconciliations",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "occupancy_registration_status_histories",
                schema: "leasing");

            migrationBuilder.DropTable(
                name: "occupancy_registration_vehicle_assignments",
                schema: "leasing");

            migrationBuilder.DropTable(
                name: "occupancy_registration_worker_assignments",
                schema: "leasing");

            migrationBuilder.DropTable(
                name: "occupancy_registrations",
                schema: "leasing");

            migrationBuilder.DropTable(
                name: "parcel_custody_events",
                schema: "security");

            migrationBuilder.DropTable(
                name: "parcels",
                schema: "security");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "platform_audit_log",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_refresh_tokens",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_role_permissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_roles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_user_role_assignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_users",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "pool_incidents",
                schema: "amenities");

            migrationBuilder.DropTable(
                name: "pool_sessions",
                schema: "amenities");

            migrationBuilder.DropTable(
                name: "rate_plans",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "rate_slabs",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "readings",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "resident_accounts",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "residents",
                schema: "residents");

            migrationBuilder.DropTable(
                name: "role_assignments",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "seba_visit_details",
                schema: "security");

            migrationBuilder.DropTable(
                name: "service_charge_rules",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "service_provider_assignments",
                schema: "security");

            migrationBuilder.DropTable(
                name: "service_provider_profiles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "staff_members",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "tenancy");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "security");
        }
    }

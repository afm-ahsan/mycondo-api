using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Utilities_Meters_RatePlans_Readings : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_id_flat_id_period",
                schema: "billing",
                table: "invoices");

            migrationBuilder.EnsureSchema(
                name: "utilities");

            // Every invoice ever generated so far (Slice E) is a service-charge invoice — backfill
            // existing rows to that value before the column becomes effectively required going
            // forward via the application layer (Invoice.Issue always sets it explicitly now).
            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "billing",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ServiceCharge");

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

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
                unique: true);

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

            // Authoritative overlap guard for RatePlan — same EXCLUDE/GiST pattern as
            // ServiceChargeRule (Slice E). btree_gist is already created by that slice's migration,
            // but CREATE EXTENSION IF NOT EXISTS is idempotent and cheap, so it's repeated here rather
            // than assumed.
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE utilities.rate_plans
                ADD CONSTRAINT ex_rate_plans_no_overlap
                EXCLUDE USING gist (
                    tenant_id WITH =,
                    building_id WITH =,
                    utility_type WITH =,
                    daterange(effective_from, effective_to, '[]') WITH &&
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meter_assignments",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "meters",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "rate_plans",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "rate_slabs",
                schema: "utilities");

            migrationBuilder.DropTable(
                name: "readings",
                schema: "utilities");

            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "billing",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_flat_id_period",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end" },
                unique: true);
        }
    }


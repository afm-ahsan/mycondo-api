using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Slice E's structural additions, scaffolded as a single migration because the whole domain layer
/// (Building.Code, Flat.AreaSqFt, ServiceChargeRule, Invoice/InvoiceLine, PaymentAllocation) was
/// built before the first `dotnet ef migrations add` of this slice, so EF diffed them all at once.
/// Adds two columns to existing `property` tables (`buildings.code`, `flats.area_sq_ft`) and three
/// new `billing` schema tables plus one new `payments` schema table. `billing.invoice_sequences`
/// is added separately, by hand, later in this same migration's `Up()` — it has no EF entity
/// mapping (see <c>IInvoiceSequenceRepository</c>'s doc comment), so EF's diff never sees it.
/// </summary>
public partial class Add_SliceE_Property_Billing_Payments_Tables : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.AddColumn<decimal>(
                name: "area_sq_ft",
                schema: "property",
                table: "flats",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "property",
                table: "buildings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.CreateIndex(
                name: "ux_buildings_tenant_id_code",
                schema: "property",
                table: "buildings",
                columns: new[] { "tenant_id", "code" },
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
                name: "ux_invoices_tenant_id_flat_id_period",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_invoice_number",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "invoice_number" },
                unique: true);

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
                name: "ix_service_charge_rules_tenant_id_building_id",
                schema: "billing",
                table: "service_charge_rules",
                columns: new[] { "tenant_id", "building_id" });

            // billing.invoice_sequences has no EF entity mapping (see
            // IInvoiceSequenceRepository's doc comment) — a plain per-tenant/building/year counter,
            // hand-written here since EF's model diff never sees it.
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

            // Authoritative overlap guard for ServiceChargeRule — see ServiceChargeRule's doc
            // comment and IServiceChargeRuleRepository.HasOverlappingRuleAsync for the
            // application-layer pre-check that gives a friendly error before this constraint fires.
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE billing.service_charge_rules DROP CONSTRAINT IF EXISTS ex_service_charge_rules_no_overlap;
                """);

            migrationBuilder.Sql("DROP TABLE IF EXISTS billing.invoice_sequences;");

            migrationBuilder.DropTable(
                name: "invoice_lines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "service_charge_rules",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "ux_buildings_tenant_id_code",
                schema: "property",
                table: "buildings");

            migrationBuilder.DropColumn(
                name: "area_sq_ft",
                schema: "property",
                table: "flats");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "property",
                table: "buildings");
        }
    }


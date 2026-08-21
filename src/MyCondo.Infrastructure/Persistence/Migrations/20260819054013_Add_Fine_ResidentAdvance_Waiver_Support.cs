using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;
    /// <inheritdoc />
    public partial class Add_Fine_ResidentAdvance_Waiver_Support : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices");

            // No defaultValue on purpose — an EF-generated empty-string placeholder here isn't a valid
            // LedgerAccountType name; against a non-empty invoices table this should fail loudly
            // (forcing a deliberate backfill) rather than silently write invalid data. The local
            // dev/demo table has zero rows today, so this applies cleanly as-is.
            migrationBuilder.AddColumn<string>(
                name: "income_account_type",
                schema: "billing",
                table: "invoices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "responsible_flat_ownership_id",
                schema: "billing",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "responsible_occupancy_registration_id",
                schema: "billing",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsible_party_type",
                schema: "billing",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "responsible_resident_id",
                schema: "billing",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "waive_ledger_posting_id",
                schema: "billing",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "waive_reason",
                schema: "billing",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "waived_amount",
                schema: "billing",
                table: "invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "waived_at_utc",
                schema: "billing",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "waived_by",
                schema: "billing",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
                unique: true,
                filter: "source <> 'FacilityBooking' AND source <> 'Fine'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "income_account_type",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "responsible_flat_ownership_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "responsible_occupancy_registration_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "responsible_party_type",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "responsible_resident_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "waive_ledger_posting_id",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "waive_reason",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "waived_amount",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "waived_at_utc",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "waived_by",
                schema: "billing",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_tenant_id_flat_id_period_source",
                schema: "billing",
                table: "invoices",
                columns: new[] { "tenant_id", "flat_id", "period_start", "period_end", "source" },
                unique: true,
                filter: "source <> 'FacilityBooking'");
        }
    }

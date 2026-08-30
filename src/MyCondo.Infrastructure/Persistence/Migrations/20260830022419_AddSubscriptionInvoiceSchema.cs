using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSubscriptionInvoiceSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subscription_invoice_lines",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                package_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                package_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                package_version_number_snapshot = table.Column<int>(type: "integer", nullable: false),
                billing_cycle_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                base_price_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                line_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_invoice_lines", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "subscription_invoices",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                billing_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                billing_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                due_date = table.Column<DateOnly>(type: "date", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                outstanding_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                canceled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_invoices", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_subscription_invoice_lines_subscription_invoice_id",
            schema: "platform",
            table: "subscription_invoice_lines",
            column: "subscription_invoice_id");

        migrationBuilder.CreateIndex(
            name: "ix_subscription_invoices_tenant_id_status",
            schema: "platform",
            table: "subscription_invoices",
            columns: new[] { "tenant_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ux_subscription_invoices_subscription_id_period",
            schema: "platform",
            table: "subscription_invoices",
            columns: new[] { "organization_subscription_id", "billing_period_start", "billing_period_end" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_subscription_invoices_tenant_id_invoice_number",
            schema: "platform",
            table: "subscription_invoices",
            columns: new[] { "tenant_id", "invoice_number" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscription_invoice_lines",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "subscription_invoices",
            schema: "platform");
    }
}

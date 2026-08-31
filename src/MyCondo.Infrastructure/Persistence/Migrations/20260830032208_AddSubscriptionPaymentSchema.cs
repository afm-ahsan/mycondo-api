using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSubscriptionPaymentSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subscription_payments",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                reference_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_payments", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_subscription_payments_subscription_invoice_id",
            schema: "platform",
            table: "subscription_payments",
            column: "subscription_invoice_id");

        migrationBuilder.CreateIndex(
            name: "ux_subscription_payments_tenant_id_reference_number",
            schema: "platform",
            table: "subscription_payments",
            columns: new[] { "tenant_id", "reference_number" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscription_payments",
            schema: "platform");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Operations_Generator_Maintenance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
    }
}

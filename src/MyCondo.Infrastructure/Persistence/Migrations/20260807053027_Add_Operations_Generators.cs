using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Operations_Generators : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "operations");

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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "generator_sessions",
            schema: "operations");

        migrationBuilder.DropTable(
            name: "generators",
            schema: "operations");
    }
}

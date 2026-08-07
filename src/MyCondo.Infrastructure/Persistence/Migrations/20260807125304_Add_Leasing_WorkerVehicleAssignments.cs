using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Leasing_WorkerVehicleAssignments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "occupancy_registration_vehicle_assignments",
            schema: "leasing");

        migrationBuilder.DropTable(
            name: "occupancy_registration_worker_assignments",
            schema: "leasing");
    }
}

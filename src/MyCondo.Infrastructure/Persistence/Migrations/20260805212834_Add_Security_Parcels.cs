using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Security_Parcels : Migration
{
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parcel_custody_events",
                schema: "security");

            migrationBuilder.DropTable(
                name: "parcels",
                schema: "security");
        }
    }

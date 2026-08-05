using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Property_Residents_Attachments_Tables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "documents");

        migrationBuilder.EnsureSchema(
            name: "property");

        migrationBuilder.EnsureSchema(
            name: "residents");

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
            name: "buildings",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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

        migrationBuilder.CreateIndex(
            name: "ix_attachments_tenant_id_owner_type_owner_id",
            schema: "documents",
            table: "attachments",
            columns: new[] { "tenant_id", "owner_type", "owner_id" });

        migrationBuilder.CreateIndex(
            name: "ux_buildings_tenant_id_name",
            schema: "property",
            table: "buildings",
            columns: new[] { "tenant_id", "name" },
            unique: true);

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
            name: "ux_gates_tenant_id_building_id_name",
            schema: "property",
            table: "gates",
            columns: new[] { "tenant_id", "building_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_residents_tenant_id_flat_id",
            schema: "residents",
            table: "residents",
            columns: new[] { "tenant_id", "flat_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "attachments",
            schema: "documents");

        migrationBuilder.DropTable(
            name: "buildings",
            schema: "property");

        migrationBuilder.DropTable(
            name: "flats",
            schema: "property");

        migrationBuilder.DropTable(
            name: "gates",
            schema: "property");

        migrationBuilder.DropTable(
            name: "residents",
            schema: "residents");
    }
}

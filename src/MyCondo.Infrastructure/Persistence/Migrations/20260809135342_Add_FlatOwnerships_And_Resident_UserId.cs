using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_FlatOwnerships_And_Resident_UserId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "user_id",
            schema: "residents",
            table: "residents",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "flat_ownerships",
            schema: "property",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                flat_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_flat_ownerships", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_residents_tenant_id_user_id",
            schema: "residents",
            table: "residents",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "ix_flat_ownerships_tenant_id_flat_id",
            schema: "property",
            table: "flat_ownerships",
            columns: new[] { "tenant_id", "flat_id" });

        migrationBuilder.CreateIndex(
            name: "ix_flat_ownerships_tenant_id_user_id",
            schema: "property",
            table: "flat_ownerships",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "ux_flat_ownerships_tenant_id_user_id_flat_id_active",
            schema: "property",
            table: "flat_ownerships",
            columns: new[] { "tenant_id", "user_id", "flat_id" },
            unique: true,
            filter: "\"status\" = 'Active'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "flat_ownerships",
            schema: "property");

        migrationBuilder.DropIndex(
            name: "ix_residents_tenant_id_user_id",
            schema: "residents",
            table: "residents");

        migrationBuilder.DropColumn(
            name: "user_id",
            schema: "residents",
            table: "residents");
    }
}

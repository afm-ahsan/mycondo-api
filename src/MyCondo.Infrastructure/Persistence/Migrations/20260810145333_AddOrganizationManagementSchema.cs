using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOrganizationManagementSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "code",
            schema: "tenancy",
            table: "tenants",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_administrator_email",
            schema: "tenancy",
            table: "tenants",
            type: "character varying(320)",
            maxLength: 320,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "primary_administrator_full_name",
            schema: "tenancy",
            table: "tenants",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "primary_administrator_user_id",
            schema: "tenancy",
            table: "tenants",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "tenant_modules",
            schema: "tenancy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                module_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                enabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                enabled_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_modules", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_tenants_code",
            schema: "tenancy",
            table: "tenants",
            column: "code",
            unique: true,
            filter: "code IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_tenant_modules_tenant_module",
            schema: "tenancy",
            table: "tenant_modules",
            columns: new[] { "tenant_id", "module_key" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tenant_modules",
            schema: "tenancy");

        migrationBuilder.DropIndex(
            name: "ux_tenants_code",
            schema: "tenancy",
            table: "tenants");

        migrationBuilder.DropColumn(
            name: "code",
            schema: "tenancy",
            table: "tenants");

        migrationBuilder.DropColumn(
            name: "primary_administrator_email",
            schema: "tenancy",
            table: "tenants");

        migrationBuilder.DropColumn(
            name: "primary_administrator_full_name",
            schema: "tenancy",
            table: "tenants");

        migrationBuilder.DropColumn(
            name: "primary_administrator_user_id",
            schema: "tenancy",
            table: "tenants");
    }
}

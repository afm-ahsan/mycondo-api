using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Role_Code_And_RequiresBuildingScope : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "code",
            schema: "identity",
            table: "roles",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "requires_building_scope",
            schema: "identity",
            table: "roles",
            type: "boolean",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_roles_tenant_id_code",
            schema: "identity",
            table: "roles",
            columns: new[] { "tenant_id", "code" },
            unique: true,
            filter: "\"code\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_roles_tenant_id_code",
            schema: "identity",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "code",
            schema: "identity",
            table: "roles");

        migrationBuilder.DropColumn(
            name: "requires_building_scope",
            schema: "identity",
            table: "roles");
    }
}

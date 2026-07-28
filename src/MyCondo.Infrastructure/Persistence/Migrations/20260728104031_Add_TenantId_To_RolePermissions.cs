using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_TenantId_To_RolePermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            schema: "identity",
            table: "role_permissions",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_tenant_id",
            schema: "identity",
            table: "role_permissions",
            column: "tenant_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_role_permissions_tenant_id",
            schema: "identity",
            table: "role_permissions");

        migrationBuilder.DropColumn(
            name: "tenant_id",
            schema: "identity",
            table: "role_permissions");
    }
}

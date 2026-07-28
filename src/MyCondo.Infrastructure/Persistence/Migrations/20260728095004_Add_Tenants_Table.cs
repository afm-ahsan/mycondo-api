using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Tenants_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "tenancy");

        migrationBuilder.CreateTable(
            name: "tenants",
            schema: "tenancy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenants", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_tenants_slug",
            schema: "tenancy",
            table: "tenants",
            column: "slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tenants",
            schema: "tenancy");
    }
}

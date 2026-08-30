using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeatureCatalogueSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "feature_definitions",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                parent_feature_id = table.Column<Guid>(type: "uuid", nullable: true),
                module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                is_core = table.Column<bool>(type: "boolean", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                entitlement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_feature_definitions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "feature_permissions",
            schema: "platform",
            columns: table => new
            {
                feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_feature_permissions", x => new { x.feature_id, x.permission_code });
            });

        migrationBuilder.CreateIndex(
            name: "ix_feature_definitions_module",
            schema: "platform",
            table: "feature_definitions",
            column: "module");

        migrationBuilder.CreateIndex(
            name: "ix_feature_definitions_parent_feature_id",
            schema: "platform",
            table: "feature_definitions",
            column: "parent_feature_id");

        migrationBuilder.CreateIndex(
            name: "ux_feature_definitions_key",
            schema: "platform",
            table: "feature_definitions",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_feature_permissions_permission_code",
            schema: "platform",
            table: "feature_permissions",
            column: "permission_code");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "feature_definitions",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "feature_permissions",
            schema: "platform");
    }
}

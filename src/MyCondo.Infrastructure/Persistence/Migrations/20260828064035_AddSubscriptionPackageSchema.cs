using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSubscriptionPackageSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subscription_package_features",
            schema: "platform",
            columns: table => new
            {
                package_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                limit_value = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_package_features", x => new { x.package_version_id, x.feature_id });
            });

        migrationBuilder.CreateTable(
            name: "subscription_package_versions",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                package_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                effective_until = table.Column<DateOnly>(type: "date", nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                monthly_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                quarterly_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                semi_annual_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                annual_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_package_versions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "subscription_packages",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                current_version_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_packages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_subscription_package_features_feature_id",
            schema: "platform",
            table: "subscription_package_features",
            column: "feature_id");

        migrationBuilder.CreateIndex(
            name: "ux_subscription_package_versions_package_id_active",
            schema: "platform",
            table: "subscription_package_versions",
            column: "package_id",
            unique: true,
            filter: "status = 'Active'");

        migrationBuilder.CreateIndex(
            name: "ux_subscription_package_versions_package_id_version",
            schema: "platform",
            table: "subscription_package_versions",
            columns: new[] { "package_id", "version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_subscription_packages_code",
            schema: "platform",
            table: "subscription_packages",
            column: "code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscription_package_features",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "subscription_package_versions",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "subscription_packages",
            schema: "platform");
    }
}

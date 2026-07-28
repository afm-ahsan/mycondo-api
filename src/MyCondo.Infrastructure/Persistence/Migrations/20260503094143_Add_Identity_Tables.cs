using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Identity_Tables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "permissions",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                is_building_scopable = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_permissions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_assignments",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                building_id = table.Column<Guid>(type: "uuid", nullable: true),
                granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_assignments", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            schema: "identity",
            columns: table => new
            {
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                granted_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
            });

        migrationBuilder.CreateTable(
            name: "roles",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                is_system = table.Column<bool>(type: "boolean", nullable: false),
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
                table.PrimaryKey("pk_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_permissions_name",
            schema: "identity",
            table: "permissions",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_tenant_id_user_id",
            schema: "identity",
            table: "refresh_tokens",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "ux_refresh_tokens_token_hash",
            schema: "identity",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_role_assignments_tenant_id_user_id",
            schema: "identity",
            table: "role_assignments",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "ux_role_assignments_user_role_building",
            schema: "identity",
            table: "role_assignments",
            columns: new[] { "tenant_id", "user_id", "role_id", "building_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_permission_id",
            schema: "identity",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ux_roles_tenant_id_name",
            schema: "identity",
            table: "roles",
            columns: new[] { "tenant_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_users_tenant_id_email",
            schema: "identity",
            table: "users",
            columns: new[] { "tenant_id", "email" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "permissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "refresh_tokens",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "role_assignments",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "role_permissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "roles",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "users",
            schema: "identity");
    }
}

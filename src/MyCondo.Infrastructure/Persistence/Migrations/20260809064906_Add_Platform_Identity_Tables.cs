using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Add_Platform_Identity_Tables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "platform");

        migrationBuilder.CreateTable(
            name: "platform_audit_log",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                target_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                target_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: true),
                correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_audit_log", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_refresh_tokens",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                table.PrimaryKey("pk_platform_refresh_tokens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_role_permissions",
            schema: "platform",
            columns: table => new
            {
                platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                granted_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_role_permissions", x => new { x.platform_role_id, x.permission_id });
            });

        migrationBuilder.CreateTable(
            name: "platform_roles",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                is_system = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_user_role_assignments",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                platform_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_user_role_assignments", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_users",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                last_login_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_platform_audit_log_actor_platform_user_id",
            schema: "platform",
            table: "platform_audit_log",
            column: "actor_platform_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_platform_audit_log_occurred_at_utc",
            schema: "platform",
            table: "platform_audit_log",
            column: "occurred_at_utc");

        migrationBuilder.CreateIndex(
            name: "ix_platform_refresh_tokens_platform_user_id",
            schema: "platform",
            table: "platform_refresh_tokens",
            column: "platform_user_id");

        migrationBuilder.CreateIndex(
            name: "ux_platform_refresh_tokens_token_hash",
            schema: "platform",
            table: "platform_refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_platform_role_permissions_permission_id",
            schema: "platform",
            table: "platform_role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ux_platform_roles_name",
            schema: "platform",
            table: "platform_roles",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_platform_user_role_assignments_user_id_role_id",
            schema: "platform",
            table: "platform_user_role_assignments",
            columns: ["platform_user_id", "platform_role_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_platform_users_email",
            schema: "platform",
            table: "platform_users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "platform_audit_log",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "platform_refresh_tokens",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "platform_role_permissions",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "platform_roles",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "platform_user_role_assignments",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "platform_users",
            schema: "platform");
    }
}

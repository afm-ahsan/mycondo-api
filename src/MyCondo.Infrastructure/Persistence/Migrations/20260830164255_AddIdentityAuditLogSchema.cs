using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds the tenant-scoped identity audit log (mycondo-docs ADR-035) — user/role administrative
/// mutations and privileged-target denials. Same append-only shape as
/// <c>finance.finance_audit_log</c>, in the <c>identity</c> schema alongside the rest of the tenant
/// identity aggregate.
/// </summary>
public partial class AddIdentityAuditLogSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "identity_audit_log",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                target_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                target_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: true),
                correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_identity_audit_log", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_identity_audit_log_tenant_id_occurred_at_utc",
            schema: "identity",
            table: "identity_audit_log",
            columns: new[] { "tenant_id", "occurred_at_utc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "identity_audit_log",
            schema: "identity");
    }
}

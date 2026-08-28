using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOrganizationSubscriptionAndTenantFeatureOverrideSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "organization_subscriptions",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                package_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                end_date = table.Column<DateOnly>(type: "date", nullable: true),
                next_billing_date = table.Column<DateOnly>(type: "date", nullable: true),
                base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                effective_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                restricted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                canceled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                auto_renew = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_subscriptions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tenant_feature_overrides",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                effective_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_feature_overrides", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_organization_subscriptions_tenant_id_current",
            schema: "platform",
            table: "organization_subscriptions",
            column: "tenant_id",
            unique: true,
            filter: "status IN ('Active', 'PastDue', 'Restricted')");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_feature_overrides_tenant_id_feature_id",
            schema: "platform",
            table: "tenant_feature_overrides",
            columns: new[] { "tenant_id", "feature_id" });

        // Authoritative overlap guard (ADR-033 §12/§22 of Task 03) — at most one TenantFeatureOverride
        // may be simultaneously effective for a given (tenant_id, feature_id) pair, so the resolver's
        // "look up A TenantFeatureOverride whose window contains now" (ADR-033 §13 step 4) is never
        // ambiguous. Same EXCLUDE USING gist / btree_gist mechanism already used for
        // billing.service_charge_rules / utilities.rate_plans / amenities.bookings (see
        // InitialPlatformSchema) — EF Core has no fluent-API representation for it. tstzrange's default
        // bounds ('[)') match the ADR's own "[EffectiveFrom, EffectiveUntil)" half-open notation; a
        // null effective_until yields an unbounded upper range, which is exactly "still open-ended."
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            ALTER TABLE platform.tenant_feature_overrides
            ADD CONSTRAINT ex_tenant_feature_overrides_no_overlap
            EXCLUDE USING gist (
                tenant_id WITH =,
                feature_id WITH =,
                tstzrange(effective_from, effective_until) WITH &&
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE platform.tenant_feature_overrides DROP CONSTRAINT IF EXISTS ex_tenant_feature_overrides_no_overlap;
            """);

        migrationBuilder.DropTable(
            name: "organization_subscriptions",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "tenant_feature_overrides",
            schema: "platform");
    }
}

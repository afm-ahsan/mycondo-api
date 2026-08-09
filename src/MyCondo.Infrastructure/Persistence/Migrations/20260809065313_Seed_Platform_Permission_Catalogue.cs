using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCondo.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the Platform-scope permission catalogue into the existing, genuinely global
/// <c>identity.permissions</c> table (confirmed to carry no tenant_id and no RLS policy — see
/// mycondo-docs ADR-019) rather than a separate PlatformPermission table. Module "platform"
/// (lowercase, matching every other Module value in this table) is what
/// PlatformBootstrapSeeder.GetByModuleAsync filters on to grant the Platform SuperAdmin role. None of
/// these are building-scopable — there is no building dimension at Platform scope.
///
/// "platform.organization.update" and "platform.organization.reactivate" are seeded for forward
/// compatibility (per the approved Phase 1 blueprint's illustrative permission list) but no Phase 1
/// endpoint checks either yet — see PlatformOrganizationEndpoints' doc comment for why (no domain
/// support for tenant rename, and Tenant.Activate() explicitly rejects Suspended -> Active today).
/// "platform.organization.activate" is an addition beyond the blueprint's illustrative list, needed so
/// "create organization" doesn't leave a tenant permanently stuck in PendingActivation.
/// </summary>
public partial class Seed_Platform_Permission_Catalogue : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "permissions",
            columns: ["id", "description", "module", "is_building_scopable", "name"],
            values: new object[,]
            {
                { new System.Guid("d7f813ba-bf9f-4347-b6c5-e04e09c32094"), "View organization (tenant) metadata", "platform", false, "platform.organization.read" },
                { new System.Guid("b05dac36-94df-490b-a4d4-5395012c14d2"), "Provision new organizations", "platform", false, "platform.organization.create" },
                { new System.Guid("6b4042d4-5993-4de7-8a5e-5de33b6d426e"), "Update organization metadata", "platform", false, "platform.organization.update" },
                { new System.Guid("aa75e24c-c902-45c0-b75b-1ff9efe4003c"), "Suspend organizations", "platform", false, "platform.organization.suspend" },
                { new System.Guid("db63cd85-d597-4bd4-be95-304cc4cde570"), "Activate a newly provisioned organization", "platform", false, "platform.organization.activate" },
                { new System.Guid("c0d259c6-1605-4154-952b-0318032ae123"), "Reactivate a suspended organization", "platform", false, "platform.organization.reactivate" },
                { new System.Guid("dc78d7c2-5e59-4d93-a762-5d52bb32bdc2"), "View subscription/plan information", "platform", false, "platform.subscription.read" },
                { new System.Guid("94a9f657-de43-40e3-8727-95811d1af18f"), "Manage subscriptions/plans", "platform", false, "platform.subscription.manage" },
                { new System.Guid("f636be2c-87ab-4cea-bce3-eac100570670"), "Perform controlled cross-tenant support operations", "platform", false, "platform.support.access" },
                { new System.Guid("07c657a2-db0e-49d7-96cf-cc389e25de1b"), "View platform-level audit records", "platform", false, "platform.audit.read" },
                { new System.Guid("806f4114-81d8-43ac-b93d-2e5a9de52710"), "View platform diagnostics", "platform", false, "platform.diagnostics.read" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM identity.permissions WHERE module = 'platform';");
    }
}

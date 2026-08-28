using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Application.Common.Authorization;

/// <summary>
/// The single source of truth for the commercial Feature Catalogue (ADR-033 §3), reused verbatim from
/// Task A's approved taxonomy (<c>NAVIGATION_FEATURE_ARCHITECTURE_AUDIT.md</c> §11) — 9 top-level groups,
/// ~30 leaf/child keys, plus the 6 legacy <c>TenantModuleKeys</c> with no current frontend/route surface
/// as <see cref="FeatureCatalogueStatus.Reserved"/> placeholders (ADR-033 §24 step 2). <c>FeatureCatalogueSeeder</c>
/// reconciles this against <c>platform.feature_definitions</c>/<c>platform.feature_permissions</c> by
/// natural key (<c>Key</c> / <c>(FeatureKey, PermissionCode)</c>), the same additive-only pattern
/// <see cref="PermissionCatalogue"/> already established for <c>identity.permissions</c>.
///
/// <c>IsCore</c> flags reproduce ADR-033 §6's explicit list exactly — every group root defaults to
/// non-core unless individually named there (<c>property</c> and <c>administration</c> are the two group
/// roots ADR-033 names directly; <c>residents</c>, <c>leasing</c>, and <c>finance</c> are not, even though
/// several of their children are).
///
/// <see cref="PermissionMappings"/> is administrative/UX metadata only (ADR-033 §5) — never read by
/// backend enforcement. Every <c>PermissionCode</c> below is verified against
/// <see cref="PermissionCatalogue.Entries"/> at the current catalogue snapshot; nothing here invents a
/// permission that does not already exist. Known gaps, deliberately left unmapped rather than forcing an
/// inexact mapping (per this task's "document the gap rather than manufacture a mapping" instruction):
///
/// <list type="bullet">
/// <item><c>sebavisitor.*</c> has no corresponding Feature Catalogue leaf at all in Task A's approved §11
/// list (Seba office visitor tracking was not one of the 8 <c>security.*</c> children). Not invented here —
/// flagged for the Feature Catalogue's own future maintainers to decide (new leaf vs. folding into an
/// existing one) rather than silently expanding the approved taxonomy.</item>
/// <item><c>tenant.view</c>/<c>tenant.manage</c> are legacy tenant-scheme permissions from the
/// pre-ADR-025 design (Task 00's audit: the privilege-escalation bug they were part of is already fixed) —
/// not part of any tenant-facing commercial feature, left unmapped.</item>
/// <item><c>platform.*</c> permissions are Platform-tier only; Platform-scheme requests never implement
/// <c>IRequiresFeature</c> (ADR-033 §16), so none are mapped to a tenant Feature Catalogue row.</item>
/// <item><c>lease.view</c>/<c>lease.manage</c> look superseded by <c>occupancy-registration.*</c> (which
/// backs <c>leasing.tenant_registration</c>) but are still present in the catalogue with no orphan marker —
/// left unmapped rather than guessed at.</item>
/// <item><c>complaint.*</c>/<c>notification.*</c>/<c>document.*</c>/<c>workorder.*</c> correspond
/// conceptually to the Reserved <c>complaints</c>/<c>notifications</c>/<c>documents</c>/<c>maintenance</c>
/// placeholders below, but Reserved features are not commercially active — no <see cref="FeaturePermission"/>
/// row is created for a Reserved feature in this task.</item>
/// <item><c>property.buildings</c> / <c>property.flats</c> and <c>utilities.electricity</c> /
/// <c>utilities.gas</c> share one underlying permission set each (<c>property.*</c>, <c>utility.*</c>) —
/// today's RBAC granularity does not yet distinguish the two children of either pair, so the shared set is
/// mapped to both leaves rather than arbitrarily picking one.</item>
/// </list>
/// </summary>
public static class FeatureCatalogue
{
    public static readonly (
        string Key,
        string Name,
        string? Description,
        string? ParentKey,
        string Module,
        FeatureCatalogueStatus Status,
        bool IsCore,
        int DisplayOrder,
        FeatureEntitlementType EntitlementType)[] Entries =
    [
        // property
        ("property", "Property", "Buildings and flats", null, "property", FeatureCatalogueStatus.Active, true, 100, FeatureEntitlementType.Boolean),
        ("property.buildings", "Buildings", null, "property", "property", FeatureCatalogueStatus.Active, false, 101, FeatureEntitlementType.Boolean),
        ("property.flats", "Flats", null, "property", "property", FeatureCatalogueStatus.Active, false, 102, FeatureEntitlementType.Boolean),

        // residents
        ("residents", "Residents", null, null, "residents", FeatureCatalogueStatus.Active, false, 200, FeatureEntitlementType.Boolean),
        ("residents.directory", "Resident Directory", null, "residents", "residents", FeatureCatalogueStatus.Active, true, 201, FeatureEntitlementType.Boolean),
        ("residents.flat_owners", "Flat Owners", null, "residents", "residents", FeatureCatalogueStatus.Active, true, 202, FeatureEntitlementType.Boolean),
        ("residents.self_service", "Resident Self-Service (My Flats / My Invoices)", "Core self-service capability flowing from a valid ownership/occupancy relationship — never a paid feature (ADR-033 §6, Task A §0.5 decision 4).", "residents", "residents", FeatureCatalogueStatus.Active, true, 203, FeatureEntitlementType.Boolean),

        // leasing
        ("leasing", "Leasing", null, null, "leasing", FeatureCatalogueStatus.Active, false, 300, FeatureEntitlementType.Boolean),
        ("leasing.tenant_registration", "Tenant Registration", null, "leasing", "leasing", FeatureCatalogueStatus.Active, true, 301, FeatureEntitlementType.Boolean),

        // security
        ("security", "Security & Front Desk", null, null, "security", FeatureCatalogueStatus.Active, false, 400, FeatureEntitlementType.Boolean),
        ("security.directory", "Security Directory", null, "security", "security", FeatureCatalogueStatus.Active, false, 401, FeatureEntitlementType.Boolean),
        ("security.gates", "Entry Gates", null, "security", "security", FeatureCatalogueStatus.Active, false, 402, FeatureEntitlementType.Boolean),
        ("security.visitors", "Visitors", null, "security", "security", FeatureCatalogueStatus.Active, false, 403, FeatureEntitlementType.Boolean),
        ("security.vehicles", "Vehicles", null, "security", "security", FeatureCatalogueStatus.Active, false, 404, FeatureEntitlementType.Boolean),
        ("security.domestic_workers", "Domestic Workers", null, "security", "security", FeatureCatalogueStatus.Active, false, 405, FeatureEntitlementType.Boolean),
        ("security.service_providers", "Service Providers", null, "security", "security", FeatureCatalogueStatus.Active, false, 406, FeatureEntitlementType.Boolean),
        ("security.staff_attendance", "Staff Attendance", null, "security", "security", FeatureCatalogueStatus.Active, false, 407, FeatureEntitlementType.Boolean),
        ("security.parcels", "Parcels", null, "security", "security", FeatureCatalogueStatus.Active, false, 408, FeatureEntitlementType.Boolean),

        // facilities
        ("facilities", "Facilities", null, null, "facilities", FeatureCatalogueStatus.Active, false, 500, FeatureEntitlementType.Boolean),
        ("facilities.community_hall", "Community Hall", null, "facilities", "facilities", FeatureCatalogueStatus.Active, false, 501, FeatureEntitlementType.Boolean),
        ("facilities.swimming_pool", "Swimming Pool", null, "facilities", "facilities", FeatureCatalogueStatus.Active, false, 502, FeatureEntitlementType.Boolean),
        ("facilities.reports", "Facilities Reports", null, "facilities", "facilities", FeatureCatalogueStatus.Active, false, 503, FeatureEntitlementType.Boolean),

        // utilities
        ("utilities", "Utilities", null, null, "utilities", FeatureCatalogueStatus.Active, false, 600, FeatureEntitlementType.Boolean),
        ("utilities.electricity", "Electricity", null, "utilities", "utilities", FeatureCatalogueStatus.Active, false, 601, FeatureEntitlementType.Boolean),
        ("utilities.gas", "Gas", null, "utilities", "utilities", FeatureCatalogueStatus.Active, false, 602, FeatureEntitlementType.Boolean),
        ("utilities.reports", "Utilities Reports", null, "utilities", "utilities", FeatureCatalogueStatus.Active, false, 603, FeatureEntitlementType.Boolean),

        // operations
        ("operations", "Operations", null, null, "operations", FeatureCatalogueStatus.Active, false, 700, FeatureEntitlementType.Boolean),
        ("operations.generator", "Generator", null, "operations", "operations", FeatureCatalogueStatus.Active, false, 701, FeatureEntitlementType.Boolean),
        ("operations.gas_cylinders", "Gas Cylinders", null, "operations", "operations", FeatureCatalogueStatus.Active, false, 702, FeatureEntitlementType.Boolean),

        // finance
        ("finance", "Finance & Accounting", null, null, "finance", FeatureCatalogueStatus.Active, false, 800, FeatureEntitlementType.Boolean),
        ("finance.billing", "Billing", null, "finance", "finance", FeatureCatalogueStatus.Active, true, 801, FeatureEntitlementType.Boolean),
        ("finance.payments", "Payments", null, "finance", "finance", FeatureCatalogueStatus.Active, true, 802, FeatureEntitlementType.Boolean),
        ("finance.expenses", "Expenses", null, "finance", "finance", FeatureCatalogueStatus.Active, true, 803, FeatureEntitlementType.Boolean),
        ("finance.resident_ledger", "Resident Ledger", null, "finance", "finance", FeatureCatalogueStatus.Active, true, 804, FeatureEntitlementType.Boolean),
        ("finance.banking", "Banking & Investments", "Premium capability — Financial Accounts, Fixed Deposits, Bank Reconciliation (Task A §13).", "finance", "finance", FeatureCatalogueStatus.Active, false, 805, FeatureEntitlementType.Boolean),
        ("finance.financial_accounts", "Financial Accounts", null, "finance.banking", "finance", FeatureCatalogueStatus.Active, false, 806, FeatureEntitlementType.Boolean),
        ("finance.fixed_deposits", "Fixed Deposits", null, "finance.banking", "finance", FeatureCatalogueStatus.Active, false, 807, FeatureEntitlementType.Boolean),
        ("finance.bank_reconciliation", "Bank Reconciliation", null, "finance.banking", "finance", FeatureCatalogueStatus.Active, false, 808, FeatureEntitlementType.Boolean),
        ("finance.reports", "Finance Reports", null, "finance", "finance", FeatureCatalogueStatus.Active, true, 809, FeatureEntitlementType.Boolean),
        ("finance.governance", "Governance", "Compliance-critical — accounting periods, chart of accounts, journal entries, audit (Task A §12).", "finance", "finance", FeatureCatalogueStatus.Active, true, 810, FeatureEntitlementType.Boolean),

        // administration
        ("administration", "Administration", "Internal / non-sellable — required for the product to function at all (Task A §13).", null, "administration", FeatureCatalogueStatus.Active, true, 900, FeatureEntitlementType.Boolean),
        ("administration.users", "Users", null, "administration", "administration", FeatureCatalogueStatus.Active, true, 901, FeatureEntitlementType.Boolean),
        ("administration.roles", "Roles", null, "administration", "administration", FeatureCatalogueStatus.Active, true, 902, FeatureEntitlementType.Boolean),

        // Reserved — legacy TenantModuleKeys with no current frontend/route surface (Task A §0.5
        // decision 6 / ADR-033 §24 step 2). Representable, never enabled in any package/override until a
        // roadmap decision promotes one to Active. Not repurposed or removed speculatively.
        ("vendors", "Vendors (Reserved)", null, null, "vendors", FeatureCatalogueStatus.Reserved, false, 990, FeatureEntitlementType.Boolean),
        ("payroll", "Payroll (Reserved)", null, null, "payroll", FeatureCatalogueStatus.Reserved, false, 991, FeatureEntitlementType.Boolean),
        ("complaints", "Complaints (Reserved)", null, null, "complaints", FeatureCatalogueStatus.Reserved, false, 992, FeatureEntitlementType.Boolean),
        ("notifications", "Notifications (Reserved)", null, null, "notifications", FeatureCatalogueStatus.Reserved, false, 993, FeatureEntitlementType.Boolean),
        ("documents", "Documents (Reserved)", null, null, "documents", FeatureCatalogueStatus.Reserved, false, 994, FeatureEntitlementType.Boolean),
        ("maintenance", "Maintenance (Reserved)", null, null, "maintenance", FeatureCatalogueStatus.Reserved, false, 995, FeatureEntitlementType.Boolean),
    ];

    /// <summary>
    /// (FeatureKey, PermissionCode) pairs — administrative/UX metadata only, never read by backend
    /// enforcement (ADR-033 §5). Every <c>PermissionCode</c> is a real, currently-seeded entry in
    /// <see cref="PermissionCatalogue.Entries"/>. See this class's own doc comment for the deliberately
    /// unmapped gaps (<c>sebavisitor.*</c>, <c>lease.*</c>, <c>tenant.*</c>, <c>platform.*</c>, and the
    /// Reserved-feature-adjacent <c>complaint.*</c>/<c>notification.*</c>/<c>document.*</c>/<c>workorder.*</c>).
    /// </summary>
    public static readonly (string FeatureKey, string PermissionCode)[] PermissionMappings =
    [
        ("property", "property.view"),
        ("property", "property.create"),
        ("property", "property.update"),
        ("property", "property.delete"),

        ("residents.directory", "resident.view"),
        ("residents.directory", "resident.create"),
        ("residents.directory", "resident.update"),
        ("residents.directory", "resident.disable"),
        ("residents.flat_owners", "ownership.view"),
        ("residents.flat_owners", "ownership.manage"),
        ("residents.self_service", "invoice.view.own"),
        ("residents.self_service", "finance.report.statement.own.view"),

        ("leasing.tenant_registration", "occupancy-registration.view"),
        ("leasing.tenant_registration", "occupancy-registration.create"),
        ("leasing.tenant_registration", "occupancy-registration.owner-review"),
        ("leasing.tenant_registration", "occupancy-registration.verify"),
        ("leasing.tenant_registration", "occupancy-registration.move-out"),

        ("security.directory", "security.directory.view"),
        ("security.directory", "security.directory.household.view"),
        ("security.directory", "security.directory.worker.view"),
        ("security.directory", "security.directory.vehicle.view"),
        ("security.directory", "security.directory.detail.view"),
        ("security.gates", "gate.view"),
        ("security.gates", "gate.manage"),
        ("security.visitors", "visitor.view"),
        ("security.visitors", "visitor.create"),
        ("security.visitors", "visitor.checkin"),
        ("security.visitors", "visitor.checkout"),
        ("security.visitors", "visitor.override"),
        ("security.visitors", "visitor.block.manage"),
        ("security.vehicles", "vehicle.view"),
        ("security.vehicles", "vehicle.create"),
        ("security.vehicles", "vehicle.checkin"),
        ("security.vehicles", "vehicle.checkout"),
        ("security.vehicles", "vehicle.override"),
        ("security.vehicles", "vehicle.block.manage"),
        ("security.domestic_workers", "domesticworker.view"),
        ("security.domestic_workers", "domesticworker.manage"),
        ("security.domestic_workers", "domesticworker.assignment.manage"),
        ("security.domestic_workers", "domesticworker.checkin"),
        ("security.domestic_workers", "domesticworker.checkout"),
        ("security.domestic_workers", "domesticworker.override"),
        ("security.service_providers", "serviceprovider.view"),
        ("security.service_providers", "serviceprovider.manage"),
        ("security.service_providers", "serviceprovider.assignment.manage"),
        ("security.service_providers", "serviceprovider.checkin"),
        ("security.service_providers", "serviceprovider.checkout"),
        ("security.service_providers", "serviceprovider.override"),
        ("security.staff_attendance", "staffattendance.view"),
        ("security.staff_attendance", "staffattendance.manage"),
        ("security.staff_attendance", "staffattendance.correct"),
        ("security.staff_attendance", "staffattendance.approve"),
        ("security.parcels", "parcel.view"),
        ("security.parcels", "parcel.receive"),
        ("security.parcels", "parcel.update"),
        ("security.parcels", "parcel.notify"),
        ("security.parcels", "parcel.handover"),
        ("security.parcels", "parcel.return"),
        ("security.parcels", "parcel.escalate"),

        ("facilities.community_hall", "facility.view"),
        ("facilities.community_hall", "facility.manage"),
        ("facilities.community_hall", "facility.booking.view"),
        ("facilities.community_hall", "facility.booking.create"),
        ("facilities.community_hall", "facility.booking.approve"),
        ("facilities.community_hall", "facility.booking.cancel"),
        ("facilities.community_hall", "facility.booking.refund"),
        ("facilities.community_hall", "facility.booking.inspect"),
        ("facilities.swimming_pool", "pool.view"),
        ("facilities.swimming_pool", "pool.manage"),
        ("facilities.swimming_pool", "pool.checkin"),
        ("facilities.swimming_pool", "pool.checkout"),
        ("facilities.swimming_pool", "pool.override"),
        ("facilities.swimming_pool", "pool.incident.manage"),
        ("facilities.reports", "report.facility"),

        // utility.* permissions do not yet distinguish electricity vs. gas — the shared set is mapped
        // to both leaves (see class doc comment).
        ("utilities.electricity", "utility.meter.view"),
        ("utilities.electricity", "utility.meter.manage"),
        ("utilities.electricity", "utility.rateplan.view"),
        ("utilities.electricity", "utility.rateplan.manage"),
        ("utilities.electricity", "utility.reading.view"),
        ("utilities.electricity", "utility.reading.record"),
        ("utilities.electricity", "utility.reading.finalize"),
        ("utilities.electricity", "utility.reading.correct"),
        ("utilities.gas", "utility.meter.view"),
        ("utilities.gas", "utility.meter.manage"),
        ("utilities.gas", "utility.rateplan.view"),
        ("utilities.gas", "utility.rateplan.manage"),
        ("utilities.gas", "utility.reading.view"),
        ("utilities.gas", "utility.reading.record"),
        ("utilities.gas", "utility.reading.finalize"),
        ("utilities.gas", "utility.reading.correct"),
        ("utilities.reports", "utility.report"),

        ("operations.generator", "generator.view"),
        ("operations.generator", "generator.manage"),
        ("operations.generator", "generator.operation.manage"),
        ("operations.generator", "generator.fuel.manage"),
        ("operations.generator", "generator.maintenance.manage"),
        ("operations.generator", "generator.report"),
        ("operations.gas_cylinders", "gascylinder.view"),
        ("operations.gas_cylinders", "gascylinder.purchase.manage"),
        ("operations.gas_cylinders", "gascylinder.stock.manage"),
        ("operations.gas_cylinders", "gascylinder.approve"),
        ("operations.gas_cylinders", "gascylinder.report"),

        ("finance.billing", "billing.rule.view"),
        ("finance.billing", "billing.rule.manage"),
        ("finance.billing", "billing.generate"),
        ("finance.billing", "billing.invoice.view"),
        ("finance.billing", "billing.invoice.generate"),
        ("finance.billing", "billing.invoice.void"),
        ("finance.billing", "billing.fine.view"),
        ("finance.billing", "billing.fine.assess"),
        ("finance.billing", "billing.fine.waive"),
        ("finance.billing", "billing.fine.reverse"),
        ("finance.billing", "invoice.view"),
        ("finance.billing", "invoice.void"),
        ("finance.payments", "payment.view"),
        ("finance.payments", "payment.record"),
        ("finance.payments", "payment.reverse"),
        ("finance.expenses", "expense.view"),
        ("finance.expenses", "expense.manage"),
        ("finance.expenses", "expense.approve"),
        ("finance.expenses", "expense.pay"),
        ("finance.expenses", "expensetype.view"),
        ("finance.expenses", "expensetype.manage"),
        ("finance.expenses", "expensecategory.view"),
        ("finance.expenses", "expensecategory.manage"),
        ("finance.resident_ledger", "residentaccount.view"),
        ("finance.resident_ledger", "residentaccount.manage"),
        ("finance.financial_accounts", "finance.bankaccount.view"),
        ("finance.financial_accounts", "finance.bankaccount.manage"),
        ("finance.fixed_deposits", "finance.fixeddeposit.view"),
        ("finance.fixed_deposits", "finance.fixeddeposit.place"),
        ("finance.fixed_deposits", "finance.fixeddeposit.manage"),
        ("finance.fixed_deposits", "finance.fixeddeposit.interest.record"),
        ("finance.bank_reconciliation", "finance.reconciliation.view"),
        ("finance.bank_reconciliation", "finance.reconciliation.manage"),
        ("finance.bank_reconciliation", "finance.reconciliation.reconcile"),
        ("finance.reports", "report.financial.view"),
        ("finance.reports", "report.operational.view"),
        ("finance.reports", "finance.report.view"),
        ("finance.reports", "finance.journal.view"),
        ("finance.governance", "finance.period.manage"),
        ("finance.governance", "finance.period.close"),
        ("finance.governance", "finance.period.reopen"),
        ("finance.governance", "finance.account.view"),
        ("finance.governance", "finance.account.manage"),
        ("finance.governance", "finance.mapping.manage"),
        ("finance.governance", "finance.fund.view"),
        ("finance.governance", "finance.fund.manage"),
        ("finance.governance", "finance.journal.create"),
        ("finance.governance", "finance.journal.reverse"),
        ("finance.governance", "audit.view"),

        ("administration.users", "user.view"),
        ("administration.users", "user.create"),
        ("administration.users", "user.update"),
        ("administration.users", "user.disable"),
        ("administration.roles", "role.view"),
        ("administration.roles", "role.manage"),
        ("administration.roles", "permission.view"),
    ];
}

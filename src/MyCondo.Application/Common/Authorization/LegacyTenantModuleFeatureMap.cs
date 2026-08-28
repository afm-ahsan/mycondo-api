using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Common.Authorization;

/// <summary>
/// The single centralized mapping from every legacy <see cref="TenantModuleKeys"/> string (17 keys) to
/// its ADR-033 §24 step 2 Feature Catalogue outcome — the deterministic table ADR-033/Task 04 requires
/// instead of scattered string-switch logic across handlers/migrations. Every legacy key has exactly one
/// row here; none may be silently absent (see this project's <c>LegacyTenantModuleFeatureMapTests</c>'s
/// exhaustiveness check, which fails the moment a new <see cref="TenantModuleKeys"/> value is added
/// without a deliberate mapping decision here).
///
/// <para><b>Outcome kinds actually used (3 of the task's named taxonomy):</b></para>
/// <list type="bullet">
/// <item><see cref="LegacyModuleMappingKind.Direct"/> — the legacy key maps to exactly one Feature
/// Catalogue key (a group root or a leaf). Several of these targets are themselves
/// <c>FeatureDefinition.IsCore = true</c> (<c>property</c>, <c>finance.billing</c>,
/// <c>finance.payments</c>, <c>finance.expenses</c>) — that is this taxonomy's "Core / No Commercial
/// Mapping" case, represented here as a <see cref="LegacyModuleMapping.TargetIsCore"/> flag on an
/// otherwise-ordinary Direct row rather than as a separate mapping mechanism, since core-ness is a
/// property of the target <see cref="FeatureCatalogue"/> entry, not a different kind of mapping. The
/// backfill seeder's grandfathered package still carries a <c>SubscriptionPackageFeature</c> row for
/// these targets — see <c>LegacyMigrationSubscriptionBackfillSeeder.EnsureGrandfatheredPackageAsync</c>'s
/// own doc comment for why (in short: <c>property</c>'s two non-core children make the core parent's row
/// load-bearing for <c>SubscriptionPackageFeatureComposition</c>'s parent-chain check, so core rows are
/// included everywhere for simplicity/uniformity rather than selectively skipped).</item>
/// <item><see cref="LegacyModuleMappingKind.ReservedCompatibility"/> — the legacy key maps 1:1 to its
/// same-named <see cref="FeatureCatalogueStatus.Reserved"/> placeholder row (<c>vendors</c>,
/// <c>payroll</c>, <c>complaints</c>, <c>notifications</c>, <c>documents</c>, <c>maintenance</c> — Task A
/// §0.5 decision 6). Reserved features can never be targeted by a package row or a
/// <c>TenantFeatureOverride</c> (<see cref="TenantFeatureOverrideEligibility"/> refuses both), so a
/// legacy tenant that had one of these enabled is preserved only as migration <em>metadata</em> (the
/// backfill seeder's <c>PlatformAuditLogEntry.Metadata</c>), never as a runtime entitlement grant — this
/// is Task 04 §8's exact instruction ("preserved through migration metadata without creating false
/// runtime entitlement semantics").</item>
/// <item><see cref="LegacyModuleMappingKind.ExplicitlyUnresolved"/> — <c>reporting</c> only. ADR-033 §24
/// step 2 already resolved this: reporting has no single Feature Catalogue equivalent because Task A
/// modeled reports as children of their owning domain (<c>finance.reports</c>,
/// <c>facilities.reports</c>, <c>utilities.reports</c>), not a cross-cutting module. A legacy
/// <c>reporting</c> row is informational only during migration — it is not translated into any
/// <see cref="FeatureCatalogue"/> target, and each domain's own <c>.reports</c> leaf inherits that
/// domain's package/override state independently.</item>
/// </list>
///
/// <para><b>Why no <c>FanOut</c> case:</b> the task's own taxonomy names a "Fan-Out Mapping" (one legacy
/// key → multiple Feature Catalogue leaves) as a possible outcome. Every one of the 17 keys was evaluated
/// against it; none required it — a Direct mapping to a Feature Catalogue <em>group root</em> (e.g.
/// <c>security</c> → <c>security</c>) already reaches every descendant leaf once the backfill seeder
/// enables the full Active subtree under that root (see the seeder's own doc comment for why the full
/// subtree, not just the named root, is what "preserve current access" requires). A dedicated enum case
/// with zero members and zero test coverage would be exactly the "half-finished implementation"/dead
/// branch this codebase's conventions ask to avoid — documented here instead of represented in code.</para>
/// </summary>
public enum LegacyModuleMappingKind
{
    Direct,
    ReservedCompatibility,
    ExplicitlyUnresolved,
}

public sealed record LegacyModuleMapping(
    string LegacyModuleKey,
    LegacyModuleMappingKind Kind,
    string? TargetFeatureKey,
    bool TargetIsCore,
    string Rationale);

public static class LegacyTenantModuleFeatureMap
{
    public static readonly LegacyModuleMapping[] Mappings =
    [
        new("property", LegacyModuleMappingKind.Direct, "property", TargetIsCore: true,
            "1:1 group-root mapping; 'property' is IsCore=true in the Feature Catalogue, so this is also the taxonomy's Core/No-Commercial-Mapping case — no package row needed."),

        new("billing", LegacyModuleMappingKind.Direct, "finance.billing", TargetIsCore: true,
            "1:1 leaf mapping; finance.billing is IsCore=true (ADR-033 §6) — Core/No-Commercial-Mapping case."),

        new("payments", LegacyModuleMappingKind.Direct, "finance.payments", TargetIsCore: true,
            "1:1 leaf mapping; finance.payments is IsCore=true (ADR-033 §6) — Core/No-Commercial-Mapping case."),

        new("expenses", LegacyModuleMappingKind.Direct, "finance.expenses", TargetIsCore: true,
            "1:1 leaf mapping; finance.expenses is IsCore=true (ADR-033 §6) — Core/No-Commercial-Mapping case."),

        new("vendors", LegacyModuleMappingKind.ReservedCompatibility, "vendors", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name. No FeaturePermission row, never package/override-eligible (TenantFeatureOverrideEligibility)."),

        new("payroll", LegacyModuleMappingKind.ReservedCompatibility, "payroll", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name."),

        new("complaints", LegacyModuleMappingKind.ReservedCompatibility, "complaints", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name."),

        new("notifications", LegacyModuleMappingKind.ReservedCompatibility, "notifications", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name."),

        new("documents", LegacyModuleMappingKind.ReservedCompatibility, "documents", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name."),

        new("reporting", LegacyModuleMappingKind.ExplicitlyUnresolved, TargetFeatureKey: null, TargetIsCore: false,
            "ADR-033 §24 step 2: no single Feature Catalogue equivalent — reports are modeled per-domain (finance.reports, facilities.reports, utilities.reports). Informational only during migration; superseded by each domain's own .reports leaf, never translated to a standalone target."),

        new("security", LegacyModuleMappingKind.Direct, "security", TargetIsCore: false,
            "1:1 group-root mapping; group root and all 8 non-core children (directory, gates, visitors, vehicles, domestic_workers, service_providers, staff_attendance, parcels) are backfilled together."),

        new("leasing", LegacyModuleMappingKind.Direct, "leasing", TargetIsCore: false,
            "1:1 group-root mapping; note leasing.tenant_registration (the only child) is itself IsCore=true, so this mapping mainly future-proofs additional leasing children."),

        new("residents", LegacyModuleMappingKind.Direct, "residents", TargetIsCore: false,
            "1:1 group-root mapping; all 3 current children (directory, flat_owners, self_service) are IsCore=true, so this mapping mainly future-proofs additional residents children."),

        new("utilities", LegacyModuleMappingKind.Direct, "utilities", TargetIsCore: false,
            "1:1 group-root mapping; group root and all 3 non-core children (electricity, gas, reports) are backfilled together."),

        new("amenities", LegacyModuleMappingKind.Direct, "facilities", TargetIsCore: false,
            "1:1 group-root mapping — legacy key name 'amenities' maps to the Feature Catalogue's 'facilities' group (Task A's naming); group root and all 3 non-core children (community_hall, swimming_pool, reports) are backfilled together."),

        new("maintenance", LegacyModuleMappingKind.ReservedCompatibility, "maintenance", TargetIsCore: false,
            "Task A §0.5 decision 6 Reserved placeholder, same key name — note this is the work-order/repair-ticket concept, distinct from the operations schema's Generator/Gas-Cylinder registers (ADR-004 addendum)."),

        new("operations", LegacyModuleMappingKind.Direct, "operations", TargetIsCore: false,
            "1:1 group-root mapping; group root and both non-core children (generator, gas_cylinders) are backfilled together."),
    ];

    public static LegacyModuleMapping GetMapping(string legacyModuleKey)
    {
        LegacyModuleMapping? mapping = Mappings.SingleOrDefault(m => m.LegacyModuleKey == legacyModuleKey);

        // A legacy key with no deliberate mapping decision is a migration-authoring defect, not a
        // runtime condition to degrade gracefully from — mirrors FeatureCatalogueSeeder's own
        // "dangling reference is a bug" stance. This is also what makes a future, un-mapped addition to
        // TenantModuleKeys fail loudly (Task 04 §32) instead of silently disappearing from migration.
        return mapping ?? throw new InvalidOperationException(
            $"LegacyTenantModuleFeatureMap has no mapping for legacy module key '{legacyModuleKey}'. " +
            "Every TenantModuleKeys value must have a deliberate, explicit mapping entry.");
    }
}

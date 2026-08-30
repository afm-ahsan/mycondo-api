using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Application.Common.Services;

/// <summary>
/// Per-tenant migration/verification calculator (ADR-033 §21/§24 steps 7–8, Task 04 §21/§34) — compares
/// what a tenant's legacy <c>TenantModule</c> rows imply against what the new normalized model actually
/// grants, so a migration loss can be caught before anything enforces access on either side.
///
/// <para><b>Not runtime authorization.</b> This is a pure, static, no-I/O function over already-loaded
/// data — the same shape as <see cref="SubscriptionPackageFeatureComposition.Validate"/> and
/// <see cref="MyCondo.Domain.Features.Platform.OrganizationSubscriptions.OrganizationSubscriptionCommercialTerms.ResolveBasePrice"/>.
/// It is never wired into the Mediator pipeline, never consulted to allow/deny a request, and is not
/// <c>ITenantEntitlementService</c> (that production resolver is explicitly out of this task's scope,
/// deferred to ADR-035). Used only by <c>LegacyMigrationSubscriptionBackfillSeeder</c> (to log/audit a
/// per-tenant comparison at backfill time) and by tests.</para>
///
/// <para><b>NewEffective delegates to <see cref="TenantEntitlementResolution.Resolve"/></b> — the same
/// precedence function <c>TenantEntitlementService</c> (ADR-033 §13, Task 05) uses — applied to an
/// in-memory catalogue/package/override snapshot instead of a live database lookup, so this simulation
/// and the production resolver can never independently drift (ADR-033 §22/§29).</para>
/// </summary>
public sealed record ShadowComparisonResult(
    Guid TenantId,
    IReadOnlySet<string> LegacyExpectedFeatureKeys,
    IReadOnlySet<string> NewEffectiveFeatureKeys,
    IReadOnlySet<string> LostFeatureKeys,
    IReadOnlySet<string> GainedFeatureKeys)
{
    /// <summary>A non-empty <see cref="LostFeatureKeys"/> means the migration under-entitles this
    /// tenant relative to its legacy-implied access — the one outcome ADR-032/033's "no existing
    /// organization may lose a currently enabled capability" invariant forbids.</summary>
    public bool HasLoss => LostFeatureKeys.Count > 0;
}

public static class LegacyEntitlementShadowComparer
{
    public static ShadowComparisonResult Compare(
        Guid tenantId,
        IReadOnlyCollection<string> enabledLegacyModuleKeys,
        IReadOnlyCollection<FeatureDefinition> catalogue,
        IReadOnlyCollection<SubscriptionPackageFeature> assignedPackageFeatures,
        IReadOnlyCollection<TenantFeatureOverride> tenantOverrides,
        DateTimeOffset asOfUtc)
    {
        Dictionary<string, FeatureDefinition> catalogueByKey = catalogue.ToDictionary(f => f.Key, StringComparer.Ordinal);

        HashSet<string> legacyExpected = BuildLegacyExpected(enabledLegacyModuleKeys, catalogue, catalogueByKey);
        HashSet<string> newEffective = BuildNewEffective(catalogue, assignedPackageFeatures, tenantOverrides, asOfUtc);

        HashSet<string> lost = new(legacyExpected, StringComparer.Ordinal);
        lost.ExceptWith(newEffective);

        HashSet<string> gained = new(newEffective, StringComparer.Ordinal);
        gained.ExceptWith(legacyExpected);

        return new ShadowComparisonResult(tenantId, legacyExpected, newEffective, lost, gained);
    }

    /// <summary>
    /// Task 05 §28's post-migration verification path — compares legacy-implied expected access against
    /// the <em>real, live</em> <c>ITenantEntitlementService.GetEffectiveEntitlements</c> output for an
    /// already-migrated (grandfathered) tenant, instead of re-simulating package/override resolution.
    /// Read-only, non-authorizing, non-blocking (Task 05 §28) — a caller (test or diagnostic) supplies
    /// <paramref name="actualEffectiveEntitlements"/> already fetched from the production service.
    /// </summary>
    public static ShadowComparisonResult CompareAgainstResolver(
        Guid tenantId,
        IReadOnlyCollection<string> enabledLegacyModuleKeys,
        IReadOnlyCollection<FeatureDefinition> catalogue,
        IReadOnlyDictionary<string, bool> actualEffectiveEntitlements)
    {
        Dictionary<string, FeatureDefinition> catalogueByKey = catalogue.ToDictionary(f => f.Key, StringComparer.Ordinal);

        HashSet<string> legacyExpected = BuildLegacyExpected(enabledLegacyModuleKeys, catalogue, catalogueByKey);
        HashSet<string> newEffective = new(
            actualEffectiveEntitlements.Where(kv => kv.Value).Select(kv => kv.Key), StringComparer.Ordinal);

        HashSet<string> lost = new(legacyExpected, StringComparer.Ordinal);
        lost.ExceptWith(newEffective);

        HashSet<string> gained = new(newEffective, StringComparer.Ordinal);
        gained.ExceptWith(legacyExpected);

        return new ShadowComparisonResult(tenantId, legacyExpected, newEffective, lost, gained);
    }

    private static HashSet<string> BuildLegacyExpected(
        IReadOnlyCollection<string> enabledLegacyModuleKeys,
        IReadOnlyCollection<FeatureDefinition> catalogue,
        Dictionary<string, FeatureDefinition> catalogueByKey)
    {
        // Core features are always accessible regardless of legacy module state (ADR-033 §6) — the
        // legacy-expected baseline includes them unconditionally, exactly like the new resolver does.
        HashSet<string> legacyExpected = new(
            catalogue.Where(f => f.IsCore).Select(f => f.Key), StringComparer.Ordinal);

        foreach (string legacyKey in enabledLegacyModuleKeys)
        {
            LegacyModuleMapping mapping = LegacyTenantModuleFeatureMap.GetMapping(legacyKey);

            // ReservedCompatibility and ExplicitlyUnresolved contribute no Feature Catalogue
            // entitlement — Reserved features are never commercially active (§8), and 'reporting' has
            // no target at all (§7/ADR-033 §24 step 2).
            if (mapping.Kind != LegacyModuleMappingKind.Direct || mapping.TargetFeatureKey is null)
            {
                continue;
            }

            if (!catalogueByKey.TryGetValue(mapping.TargetFeatureKey, out FeatureDefinition? target))
            {
                continue;
            }

            foreach (FeatureDefinition descendant in ExpandActiveSubtree(target, catalogue))
            {
                legacyExpected.Add(descendant.Key);
            }
        }

        return legacyExpected;
    }

    private static HashSet<string> BuildNewEffective(
        IReadOnlyCollection<FeatureDefinition> catalogue,
        IReadOnlyCollection<SubscriptionPackageFeature> assignedPackageFeatures,
        IReadOnlyCollection<TenantFeatureOverride> tenantOverrides,
        DateTimeOffset asOfUtc)
    {
        Dictionary<FeatureDefinitionId, TenantFeatureOverride> effectiveOverrideByFeature = tenantOverrides
            .Where(o => o.EffectiveFrom <= asOfUtc && (o.EffectiveUntil is null || asOfUtc < o.EffectiveUntil.Value))
            .ToDictionary(o => o.FeatureId);

        Dictionary<FeatureDefinitionId, SubscriptionPackageFeature> packageFeatureByFeature = assignedPackageFeatures
            .ToDictionary(f => f.FeatureId);

        HashSet<string> newEffective = new(StringComparer.Ordinal);
        foreach (FeatureDefinition feature in catalogue)
        {
            effectiveOverrideByFeature.TryGetValue(feature.Id, out TenantFeatureOverride? effectiveOverride);
            packageFeatureByFeature.TryGetValue(feature.Id, out SubscriptionPackageFeature? packageFeature);

            // hasCurrentSubscription: true — this simulation always represents a package that is (or is
            // about to be) the tenant's assigned subscription, never the "no subscription at all" case
            // TenantEntitlementResolution's defensive branch exists for (see LegacyMigrationSubscriptionBackfillSeeder,
            // this comparer's only caller: it simulates the grandfathered package the tenant is about to
            // receive, not a live absent-subscription state).
            EffectiveEntitlement resolved = TenantEntitlementResolution.Resolve(
                feature, hasCurrentSubscription: true, effectiveOverride, packageFeature);

            if (resolved.Enabled)
            {
                newEffective.Add(feature.Key);
            }
        }

        return newEffective;
    }

    private static IEnumerable<FeatureDefinition> ExpandActiveSubtree(
        FeatureDefinition root, IReadOnlyCollection<FeatureDefinition> catalogue)
    {
        if (root.Status == FeatureCatalogueStatus.Active)
        {
            yield return root;
        }

        foreach (FeatureDefinition child in catalogue.Where(f => f.ParentFeatureId == root.Id))
        {
            foreach (FeatureDefinition descendant in ExpandActiveSubtree(child, catalogue))
            {
                yield return descendant;
            }
        }
    }
}

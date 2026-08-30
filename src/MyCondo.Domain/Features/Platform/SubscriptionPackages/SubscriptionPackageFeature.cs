using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>
/// Package-version-scoped feature assignment (ADR-033 §9) — belongs to the <b>version</b>, never the
/// package root, so a historical version retains exactly the feature composition it had while active.
/// Composite key (<see cref="PackageVersionId"/>, <see cref="FeatureId"/>), mirroring
/// <see cref="FeaturePermission"/>'s own composite-key join-row shape: a plain class, no aggregate-root
/// identity of its own.
///
/// Composition rules that span multiple rows — no duplicate assignment, every <see cref="FeatureId"/>
/// must be a known <see cref="FeatureDefinition"/>, a leaf's parent chain must also be enabled in the same
/// version — are enforced as a batch by <see cref="SubscriptionPackageFeatureComposition.Validate"/>, not
/// by this type's constructor, since they require the full proposed assignment set plus the Feature
/// Catalogue to evaluate.
/// </summary>
public sealed class SubscriptionPackageFeature
{
    public SubscriptionPackageVersionId PackageVersionId { get; private set; }
    public FeatureDefinitionId FeatureId { get; private set; }
    public bool Enabled { get; private set; }
    public int? LimitValue { get; private set; }

    private SubscriptionPackageFeature() { }

    public SubscriptionPackageFeature(
        SubscriptionPackageVersionId packageVersionId, FeatureDefinitionId featureId, bool enabled, int? limitValue)
    {
        if (limitValue is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limitValue), "LimitValue cannot be negative.");
        }

        PackageVersionId = packageVersionId;
        FeatureId = featureId;
        Enabled = enabled;
        LimitValue = limitValue;
    }
}

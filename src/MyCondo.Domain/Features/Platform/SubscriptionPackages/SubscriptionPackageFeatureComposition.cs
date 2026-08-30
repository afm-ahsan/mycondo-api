using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>
/// Package-authoring-time validation for a version's full proposed <see cref="SubscriptionPackageFeature"/>
/// set (ADR-033 §9/§11) — never invoked at entitlement-resolution time (the future resolver only ever does
/// a direct row lookup, ADR-033 §13, with no tree-walk). Deliberately explicit, non-inferred: enabling a
/// parent (group) feature never implicitly enables its children, and enabling a leaf requires its full
/// parent chain to carry its own <c>Enabled = true</c> row in the same proposed set.
/// </summary>
public static class SubscriptionPackageFeatureComposition
{
    public static void Validate(
        SubscriptionPackageVersionId packageVersionId,
        IReadOnlyCollection<FeatureDefinition> catalogue,
        IReadOnlyCollection<SubscriptionPackageFeature> assignments)
    {
        Dictionary<FeatureDefinitionId, FeatureDefinition> catalogueById =
            catalogue.ToDictionary(f => f.Id);

        HashSet<FeatureDefinitionId> seen = new();
        Dictionary<FeatureDefinitionId, bool> enabledById = new();

        foreach (SubscriptionPackageFeature assignment in assignments)
        {
            if (!seen.Add(assignment.FeatureId))
            {
                throw new DuplicatePackageFeatureAssignmentException(packageVersionId, assignment.FeatureId);
            }

            if (!catalogueById.TryGetValue(assignment.FeatureId, out FeatureDefinition? feature))
            {
                throw new UnknownPackageFeatureException(packageVersionId, assignment.FeatureId);
            }

            if (assignment.LimitValue is not null && feature.EntitlementType != FeatureEntitlementType.Numeric)
            {
                throw new InvalidPackageFeatureLimitValueException(packageVersionId, assignment.FeatureId);
            }

            enabledById[assignment.FeatureId] = assignment.Enabled;
        }

        foreach (SubscriptionPackageFeature assignment in assignments)
        {
            if (!assignment.Enabled)
            {
                continue;
            }

            FeatureDefinitionId? parentFeatureId = catalogueById[assignment.FeatureId].ParentFeatureId;
            while (parentFeatureId is not null)
            {
                if (!enabledById.TryGetValue(parentFeatureId.Value, out bool parentEnabled) || !parentEnabled)
                {
                    throw new MissingParentFeatureAssignmentException(
                        packageVersionId, assignment.FeatureId, parentFeatureId.Value);
                }

                parentFeatureId = catalogueById.TryGetValue(parentFeatureId.Value, out FeatureDefinition? parent)
                    ? parent.ParentFeatureId
                    : null;
            }
        }
    }
}

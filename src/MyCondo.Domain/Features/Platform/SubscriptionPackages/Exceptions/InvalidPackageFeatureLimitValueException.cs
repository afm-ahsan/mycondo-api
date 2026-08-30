using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>ADR-033 §9's <c>LimitValue</c> is "only meaningful when <c>FeatureDefinition.EntitlementType
/// = Numeric</c>" — a <c>LimitValue</c> set against a Boolean-entitlement feature is a rejected,
/// ambiguous package-authoring state, not silently ignored data.</summary>
public sealed class InvalidPackageFeatureLimitValueException(SubscriptionPackageVersionId packageVersionId, FeatureDefinitionId featureId)
    : DomainException(
        $"Subscription package version {packageVersionId} sets a LimitValue for feature {featureId}, " +
        "which is not a Numeric-entitlement feature.");

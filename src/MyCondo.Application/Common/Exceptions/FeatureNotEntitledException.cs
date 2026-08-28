namespace MyCondo.Application.Common.Exceptions;

/// <summary>
/// The current organization's subscription does not include <see cref="FeatureKey"/> (ADR-033 §16) —
/// distinct from <see cref="ForbiddenException"/> so the API can map it to a dedicated
/// <c>feature_not_entitled</c> error code instead of the generic RBAC "you don't have permission" case.
/// </summary>
public sealed class FeatureNotEntitledException : ApplicationException
{
    public string FeatureKey { get; }

    public FeatureNotEntitledException(string featureKey)
        : base($"Feature '{featureKey}' is not entitled for this organization.")
    {
        FeatureKey = featureKey;
    }
}

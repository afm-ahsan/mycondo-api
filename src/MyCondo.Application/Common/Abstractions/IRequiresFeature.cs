namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Marks a Mediator request as gated by the commercial Feature Catalogue (ADR-033 §16). Declared
/// directly on the request type — not a reflection-read attribute — so the request→feature association
/// is visible at the call site and no reflection pass is needed on every request.
///
/// <see cref="FeatureKey"/> must be a stable <c>FeatureDefinition.Key</c> from
/// <c>MyCondo.Application.Common.Authorization.FeatureCatalogue.Entries</c> whose
/// <c>Status</c> is <c>Active</c> and <c>IsCore</c> is <c>false</c> — a request behind a core feature or
/// with no catalogue row at all has nothing to gate and must not implement this interface (ADR-033 §6).
/// </summary>
public interface IRequiresFeature
{
    string FeatureKey { get; }
}

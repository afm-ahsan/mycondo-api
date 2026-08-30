namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Derives the canonical Feature Catalogue key for a request marked <see cref="IRequiresResolvedFeature"/>
/// (ADR-033 Task 09A) by loading the tenant-owned resource the request identifies (never a request-supplied
/// type/category string — that would let the client pick its own entitlement check).
/// </summary>
public interface IRequestFeatureResolver<in TRequest>
{
    /// <summary>
    /// Throws <see cref="Exceptions.NotFoundException"/> when the referenced resource does not exist or is
    /// not owned by <paramref name="tenantId"/>, preserving the same not-found semantics the handler would
    /// otherwise produce; throws <see cref="InvalidOperationException"/> when the resource's classification
    /// has no approved Active Feature Catalogue mapping (a configuration/invariant defect, never a silent
    /// grant).
    /// </summary>
    Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken);
}

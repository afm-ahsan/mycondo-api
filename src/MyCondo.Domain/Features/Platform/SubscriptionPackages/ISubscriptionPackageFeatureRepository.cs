namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public interface ISubscriptionPackageFeatureRepository
{
    Task<List<SubscriptionPackageFeature>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>The feature composition assigned to one package version — used by the entitlement
    /// resolver (ADR-033 §13/§21/§23) so it loads only the tenant's own current subscription's
    /// composition, not every package version in the catalogue.</summary>
    Task<List<SubscriptionPackageFeature>> GetForPackageVersionAsync(
        SubscriptionPackageVersionId packageVersionId, CancellationToken cancellationToken);

    void Add(SubscriptionPackageFeature packageFeature);
}

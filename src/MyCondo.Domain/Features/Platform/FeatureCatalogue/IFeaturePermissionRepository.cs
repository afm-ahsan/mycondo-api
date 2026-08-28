namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

public interface IFeaturePermissionRepository
{
    Task<List<FeaturePermission>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Used by the Feature Catalogue seeder to add mappings missing from
    /// <c>platform.feature_permissions</c>. Never used to update or remove an existing row —
    /// reconciliation is additive-only.</summary>
    void Add(FeaturePermission featurePermission);
}

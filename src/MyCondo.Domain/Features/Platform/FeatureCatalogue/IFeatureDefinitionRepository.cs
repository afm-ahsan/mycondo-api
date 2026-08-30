namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

public interface IFeatureDefinitionRepository
{
    Task<List<FeatureDefinition>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Used by the Feature Catalogue seeder to add rows missing from <c>platform.feature_definitions</c>.
    /// Never used to update or remove an existing row — reconciliation is additive-only.</summary>
    void Add(FeatureDefinition featureDefinition);
}

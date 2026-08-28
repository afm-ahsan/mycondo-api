using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FeatureDefinitionRepository(MyCondoDbContext db) : IFeatureDefinitionRepository
{
    public Task<List<FeatureDefinition>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<FeatureDefinition>().OrderBy(f => f.DisplayOrder).ToListAsync(cancellationToken);

    public void Add(FeatureDefinition featureDefinition) => db.Set<FeatureDefinition>().Add(featureDefinition);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FeaturePermissionRepository(MyCondoDbContext db) : IFeaturePermissionRepository
{
    public Task<List<FeaturePermission>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<FeaturePermission>().ToListAsync(cancellationToken);

    public void Add(FeaturePermission featurePermission) => db.Set<FeaturePermission>().Add(featurePermission);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPackageFeatureRepository(MyCondoDbContext db) : ISubscriptionPackageFeatureRepository
{
    public Task<List<SubscriptionPackageFeature>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<SubscriptionPackageFeature>().ToListAsync(cancellationToken);

    public void Add(SubscriptionPackageFeature packageFeature) => db.Set<SubscriptionPackageFeature>().Add(packageFeature);
}

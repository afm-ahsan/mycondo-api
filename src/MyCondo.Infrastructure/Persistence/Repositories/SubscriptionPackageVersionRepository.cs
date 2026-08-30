using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPackageVersionRepository(MyCondoDbContext db) : ISubscriptionPackageVersionRepository
{
    public Task<List<SubscriptionPackageVersion>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<SubscriptionPackageVersion>().ToListAsync(cancellationToken);

    public void Add(SubscriptionPackageVersion version) => db.Set<SubscriptionPackageVersion>().Add(version);
}

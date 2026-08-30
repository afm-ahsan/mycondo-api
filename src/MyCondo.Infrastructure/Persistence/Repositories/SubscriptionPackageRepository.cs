using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPackageRepository(MyCondoDbContext db) : ISubscriptionPackageRepository
{
    public Task<List<SubscriptionPackage>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<SubscriptionPackage>().ToListAsync(cancellationToken);

    public void Add(SubscriptionPackage package) => db.Set<SubscriptionPackage>().Add(package);
}

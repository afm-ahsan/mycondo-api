using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class OrganizationSubscriptionRepository(MyCondoDbContext db) : IOrganizationSubscriptionRepository
{
    public Task<OrganizationSubscription?> GetByIdAsync(OrganizationSubscriptionId id, CancellationToken cancellationToken) =>
        db.Set<OrganizationSubscription>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<OrganizationSubscription?> GetCurrentForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<OrganizationSubscription>()
            .Where(x =>
                x.TenantId == tenantId &&
                (x.Status == OrganizationSubscriptionStatus.Active ||
                 x.Status == OrganizationSubscriptionStatus.PastDue ||
                 x.Status == OrganizationSubscriptionStatus.Restricted))
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(OrganizationSubscription subscription) => db.Set<OrganizationSubscription>().Add(subscription);
}

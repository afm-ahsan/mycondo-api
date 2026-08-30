using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionInvoiceRepository(MyCondoDbContext db) : ISubscriptionInvoiceRepository
{
    public Task<SubscriptionInvoice?> GetByIdAsync(SubscriptionInvoiceId id, CancellationToken cancellationToken) =>
        db.Set<SubscriptionInvoice>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionInvoice>> GetForOrganizationSubscriptionAsync(
        OrganizationSubscriptionId organizationSubscriptionId, CancellationToken cancellationToken) =>
        await db.Set<SubscriptionInvoice>()
            .Where(x => x.OrganizationSubscriptionId == organizationSubscriptionId)
            .OrderByDescending(x => x.BillingPeriodStart)
            .ToListAsync(cancellationToken);

    public void Add(SubscriptionInvoice invoice) => db.Set<SubscriptionInvoice>().Add(invoice);

    public void AddLines(IEnumerable<SubscriptionInvoiceLine> lines) => db.Set<SubscriptionInvoiceLine>().AddRange(lines);
}

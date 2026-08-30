using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPaymentRepository(MyCondoDbContext db) : ISubscriptionPaymentRepository
{
    public Task<SubscriptionPayment?> GetByIdAsync(SubscriptionPaymentId id, CancellationToken cancellationToken) =>
        db.Set<SubscriptionPayment>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken cancellationToken) =>
        db.Set<SubscriptionPayment>().AnyAsync(
            x => x.TenantId == tenantId && x.ReferenceNumber == referenceNumber, cancellationToken);

    public void Add(SubscriptionPayment payment) => db.Set<SubscriptionPayment>().Add(payment);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPaymentRepository(MyCondoDbContext db) : ISubscriptionPaymentRepository
{
    public Task<SubscriptionPayment?> GetByIdAsync(SubscriptionPaymentId id, CancellationToken cancellationToken) =>
        db.Set<SubscriptionPayment>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken cancellationToken) =>
        db.Set<SubscriptionPayment>().AnyAsync(
            x => x.TenantId == tenantId && x.ReferenceNumber == referenceNumber, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPayment>> GetForInvoiceAsync(
        SubscriptionInvoiceId subscriptionInvoiceId, CancellationToken cancellationToken) =>
        await db.Set<SubscriptionPayment>().AsNoTracking()
            .Where(x => x.SubscriptionInvoiceId == subscriptionInvoiceId)
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPayment>> SearchAsync(
        Guid? tenantId, DateOnly? paymentDateFrom, DateOnly? paymentDateTo, CancellationToken cancellationToken)
    {
        IQueryable<SubscriptionPayment> query = db.Set<SubscriptionPayment>().AsNoTracking();

        if (tenantId is not null)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        if (paymentDateFrom is not null)
        {
            query = query.Where(x => x.PaymentDate >= paymentDateFrom.Value);
        }

        if (paymentDateTo is not null)
        {
            query = query.Where(x => x.PaymentDate <= paymentDateTo.Value);
        }

        return await query.OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);
    }

    public void Add(SubscriptionPayment payment) => db.Set<SubscriptionPayment>().Add(payment);
}

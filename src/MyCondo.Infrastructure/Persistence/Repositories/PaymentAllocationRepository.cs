using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PaymentAllocationRepository(MyCondoDbContext db) : IPaymentAllocationRepository
{
    public Task<IReadOnlyList<PaymentAllocation>> GetForPaymentAsync(PaymentId paymentId, CancellationToken cancellationToken) =>
        GetListAsync(a => a.PaymentId == paymentId, cancellationToken);

    public Task<IReadOnlyList<PaymentAllocation>> GetForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken) =>
        GetListAsync(a => a.InvoiceId == invoiceId, cancellationToken);

    private async Task<IReadOnlyList<PaymentAllocation>> GetListAsync(
        System.Linq.Expressions.Expression<Func<PaymentAllocation, bool>> predicate, CancellationToken cancellationToken) =>
        await db.Set<PaymentAllocation>().AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<PaymentAllocation> allocations) => db.Set<PaymentAllocation>().AddRange(allocations);
}

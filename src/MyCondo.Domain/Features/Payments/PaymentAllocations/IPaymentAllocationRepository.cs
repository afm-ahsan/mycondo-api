using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Domain.Features.Payments.PaymentAllocations;

public interface IPaymentAllocationRepository
{
    Task<IReadOnlyList<PaymentAllocation>> GetForPaymentAsync(PaymentId paymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentAllocation>> GetForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken);

    void AddRange(IEnumerable<PaymentAllocation> allocations);
}

using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.PaymentAllocations;

/// <summary>
/// One FIFO allocation of a <see cref="Payment"/> against an outstanding <see cref="Invoice"/> —
/// append-only, never updated or deleted. <see cref="FlatId"/> is denormalized from both the payment
/// and the invoice (which must agree — the handler asserts this defensively even though the
/// outstanding-invoice query is already scoped to the payment's own flat, making a cross-flat
/// allocation structurally unreachable, not just checked).
/// </summary>
public sealed class PaymentAllocation : Entity<PaymentAllocationId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public PaymentId PaymentId { get; private set; }
    public InvoiceId InvoiceId { get; private set; }
    public FlatId FlatId { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public DateTimeOffset AllocatedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PaymentAllocation() { }

    private PaymentAllocation(
        PaymentAllocationId id,
        Guid tenantId,
        PaymentId paymentId,
        InvoiceId invoiceId,
        FlatId flatId,
        decimal allocatedAmount,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        FlatId = flatId;
        AllocatedAmount = allocatedAmount;
        AllocatedAtUtc = nowUtc;
        CreatedAtUtc = nowUtc;
    }

    public static PaymentAllocation Allocate(
        Guid tenantId, PaymentId paymentId, InvoiceId invoiceId, FlatId flatId, decimal allocatedAmount, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (allocatedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "AllocatedAmount must be positive.");
        }

        return new PaymentAllocation(
            PaymentAllocationId.New(), tenantId, paymentId, invoiceId, flatId, allocatedAmount, nowUtc);
    }
}

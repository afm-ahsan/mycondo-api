using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Billing.Invoices.Exceptions;

/// <summary>
/// Thrown when voiding an invoice that already has a payment allocated to it. Voiding is restricted
/// to invoices with <c>AmountPaid == 0</c> in this slice — a paid or partially-paid invoice requires
/// coordinating an invoice-level reversal with a payment-level reversal, which is a separate,
/// not-yet-built payment-and-invoice reversal workflow.
/// </summary>
public sealed class InvoiceCannotBeVoidedException(InvoiceId invoiceId)
    : DomainException(
        $"Invoice {invoiceId} has payments allocated and cannot be voided directly. Reverse the " +
        "payment(s) first, then void — a combined reversal workflow is not yet built.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
}

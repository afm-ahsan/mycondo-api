using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Billing.Invoices.Exceptions;

/// <summary>
/// Thrown when voiding an invoice that already has a payment allocated to it, or (Billing↔Finance
/// integration template) a waiver applied. Voiding is restricted to invoices with
/// <c>AmountPaid == 0 &amp;&amp; WaivedAmount == 0</c> — a paid/partially-paid invoice requires
/// coordinating an invoice-level reversal with a payment-level reversal, and a waived/partially-waived
/// invoice requires reversing the waiver first; neither combined reversal workflow is built yet.
/// </summary>
public sealed class InvoiceCannotBeVoidedException(InvoiceId invoiceId)
    : DomainException(
        $"Invoice {invoiceId} has payments and/or a waiver applied and cannot be voided directly. " +
        "Reverse the payment(s)/waiver first, then void — a combined reversal workflow is not yet built.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
}

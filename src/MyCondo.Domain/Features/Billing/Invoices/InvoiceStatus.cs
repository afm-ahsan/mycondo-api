namespace MyCondo.Domain.Features.Billing.Invoices;

public enum InvoiceStatus
{
    Issued = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Void = 3,
    /// <summary>The entire balance was written off via <see cref="Invoice.Waive"/> with no amount
    /// ever paid. Only reachable through <see cref="Invoice.Waive"/> — Service Charge/Utility invoices
    /// never call it today, so this status is Fine-only in practice, not by a hard-coded Source check.
    /// </summary>
    Waived = 4,
    /// <summary>Part of the balance was written off via <see cref="Invoice.Waive"/> and a positive
    /// balance still remains (regardless of whether some of that balance has also been paid).</summary>
    PartiallyWaived = 5,
}

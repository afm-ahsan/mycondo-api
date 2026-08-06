namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// What generated this invoice. Added in Slice F so a utility bill (metered/fixed electricity or gas
/// charge, posted when a <c>Utilities.Readings.Reading</c> is billed) can reuse the entire
/// <see cref="Invoice"/>/<see cref="InvoiceLine"/>/payment-allocation/void machinery Slice E already
/// built, rather than duplicating it for a second charge type — see ADR-017's sibling decision for
/// Slice F, recorded alongside this change.
/// </summary>
public enum InvoiceSource
{
    ServiceCharge = 0,
    Utility = 1,
}

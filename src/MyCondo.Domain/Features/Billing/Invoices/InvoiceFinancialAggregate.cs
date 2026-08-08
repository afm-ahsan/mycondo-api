namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>TotalBilled is a period-flow value strictly within the caller's [FromDate, ToDate] window.
/// TotalOutstanding, UnpaidInvoiceCount, PartiallyPaidInvoiceCount, and OverdueInvoiceCount are current-
/// snapshot values as of the caller's AsOfDate, independent of that window.</summary>
public sealed record InvoiceFinancialAggregate(
    decimal TotalBilled,
    decimal TotalOutstanding,
    int UnpaidInvoiceCount,
    int PartiallyPaidInvoiceCount,
    int OverdueInvoiceCount
);

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>Minimal projection of one open (Issued/PartiallyPaid) invoice for receivables-ageing
/// bucketing — remaining balance, never the original <see cref="Invoice.TotalAmount"/>.</summary>
public sealed record AgeingReceivableLine(DateOnly DueDate, decimal Balance);

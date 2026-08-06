namespace MyCondo.Domain.Features.Utilities.RatePlans;

/// <summary>Unpersisted input to <see cref="RatePlan.Create"/> — analogous to
/// <c>Billing.Invoices.InvoiceLineInput</c>.</summary>
public sealed record RateSlabInput(int SlabOrder, decimal FromUnits, decimal? ToUnits, decimal RatePerUnit);

namespace MyCondo.Application.Features.Payments.DTOs;

/// <summary>TotalBilled and TotalCollected are period-flow values strictly within [FromDate, ToDate].
/// TotalOutstanding, UnpaidInvoiceCount, PartiallyPaidInvoiceCount, and OverdueInvoiceCount are
/// current-snapshot values as of <see cref="AsOfDate"/>, independent of the FromDate/ToDate window —
/// do not treat TotalOutstanding as constrained to the reporting period.</summary>
public sealed record FinancialSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    DateOnly AsOfDate,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int UnpaidInvoiceCount,
    int PartiallyPaidInvoiceCount,
    int OverdueInvoiceCount
);

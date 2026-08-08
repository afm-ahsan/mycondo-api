namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record AgeingBucketDto(
    string BucketLabel,
    int? MinDaysOverdue,
    int? MaxDaysOverdue,
    int InvoiceCount,
    decimal TotalBalance
);

/// <summary>Always 5 fixed buckets (Current, 1-30, 31-60, 61-90, 91+ days overdue), computed from
/// remaining balance as of <see cref="AsOfDate"/> — never the invoice's original amount.</summary>
public sealed record ReceivablesAgeingReportDto(
    DateOnly AsOfDate,
    IReadOnlyList<AgeingBucketDto> Buckets,
    decimal GrandTotal
);

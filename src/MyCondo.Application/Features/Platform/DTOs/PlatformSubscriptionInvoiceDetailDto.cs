namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record PlatformSubscriptionInvoiceDetailDto(
    Guid InvoiceId,
    Guid TenantId,
    string OrganizationName,
    string InvoiceNumber,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    decimal TotalAmount,
    decimal OutstandingAmount,
    string Status,
    int? DaysOverdue,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? VoidedAtUtc,
    string? VoidReason,
    DateTimeOffset? CanceledAtUtc,
    string? CancelReason,
    IReadOnlyList<PlatformSubscriptionInvoiceLineDto> Lines,
    IReadOnlyList<PlatformSubscriptionPaymentDto> Payments);

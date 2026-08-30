namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record PlatformSubscriptionInvoiceListItemDto(
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
    int? DaysOverdue);

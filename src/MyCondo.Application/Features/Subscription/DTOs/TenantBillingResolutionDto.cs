namespace MyCondo.Application.Features.Subscription.DTOs;

/// <summary>
/// One collectible CondoBD SaaS subscription invoice, narrowed for tenant-admin self-service (ADR-034
/// Task 14I) — no <c>InvoiceId</c>/<c>TenantId</c>/<c>OrganizationName</c> (the tenant already knows its
/// own identity; this is never a cross-organization list) and no Platform audit/actor metadata. Field
/// shape otherwise mirrors <c>PlatformSubscriptionInvoiceListItemDto</c> (ADR-034 Task 14D).
/// </summary>
public sealed record TenantOutstandingSubscriptionInvoiceDto(
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

/// <summary>One currency's collectible total (ADR-034 Task 14I) — currencies are never summed together;
/// see <see cref="TenantBillingResolutionDto"/>.</summary>
public sealed record TenantOutstandingCurrencySummaryDto(
    string Currency,
    decimal OutstandingAmount,
    int OutstandingInvoiceCount);

/// <summary>
/// The minimum safe tenant-facing view of the organization's own CondoBD SaaS subscription
/// billing state (ADR-032/ADR-034 Task 14I) — deliberately narrower than the Platform Control Plane's
/// <c>OrganizationSubscriptionDto</c>/<c>PlatformSubscriptionInvoiceDetailDto</c>: no package pricing,
/// discounts, other organizations' data, or platform audit/actor fields. <see cref="OutstandingSummary"/>
/// keeps every currency separate (never a synthetic cross-currency total); each invoice's DaysOverdue is
/// always backend-derived, never recalculated by the client. <see cref="SubscriptionStatus"/> is null only
/// when no subscription has ever been provisioned for this tenant — the same "never a lockout" case
/// <c>SubscriptionLifecyclePolicy.Evaluate</c> treats as Full access.
/// </summary>
public sealed record TenantBillingResolutionDto(
    string? SubscriptionStatus,
    IReadOnlyList<TenantOutstandingCurrencySummaryDto> OutstandingSummary,
    IReadOnlyList<TenantOutstandingSubscriptionInvoiceDto> OutstandingInvoices);

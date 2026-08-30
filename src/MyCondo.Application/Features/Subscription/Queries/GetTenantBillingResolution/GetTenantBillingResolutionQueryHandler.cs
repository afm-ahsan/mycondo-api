using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Subscription.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Application.Features.Subscription.Queries.GetTenantBillingResolution;

/// <summary>
/// Resolves the caller's own organization's SaaS subscription status and currently-collectible invoices
/// (ADR-032/ADR-034 Task 14I). Subscription lookup mirrors <c>SubscriptionLifecycleAccessService</c>'s
/// two-step strategy (current row, falling back to the latest terminal row) so the reported status is
/// never "no subscription" merely because the current row is Expired/Canceled. Outstanding invoices reuse
/// <see cref="ISubscriptionInvoiceRepository.GetOutstandingAsync"/> — the same bounded, tenant-scoped read
/// Task 14D's Platform collections view aggregates over — scoped here to exactly the caller's own tenant.
/// </summary>
public sealed class GetTenantBillingResolutionQueryHandler(
    ICurrentUserProvider currentUser,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionInvoiceRepository subscriptionInvoices,
    IClock clock
) : IRequestHandler<GetTenantBillingResolutionQuery, TenantBillingResolutionDto>
{
    public async ValueTask<TenantBillingResolutionDto> Handle(
        GetTenantBillingResolutionQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OrganizationSubscription? subscription =
            await organizationSubscriptions.GetCurrentForTenantAsync(tenantId, cancellationToken)
            ?? await organizationSubscriptions.GetLatestForTenantAsync(tenantId, cancellationToken);

        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        IReadOnlyList<SubscriptionInvoice> outstanding =
            await subscriptionInvoices.GetOutstandingAsync(tenantId, cancellationToken);

        List<TenantOutstandingSubscriptionInvoiceDto> invoices = outstanding
            .OrderBy(x => x.DueDate)
            .Select(x => new TenantOutstandingSubscriptionInvoiceDto(
                InvoiceNumber: x.InvoiceNumber,
                BillingPeriodStart: x.BillingPeriodStart,
                BillingPeriodEnd: x.BillingPeriodEnd,
                IssueDate: x.IssueDate,
                DueDate: x.DueDate,
                Currency: x.Currency,
                TotalAmount: x.TotalAmount,
                OutstandingAmount: x.OutstandingAmount,
                Status: x.Status.ToString(),
                DaysOverdue: SubscriptionInvoiceOverdueCalculator.DaysOverdue(x, today)))
            .ToList();

        List<TenantOutstandingCurrencySummaryDto> summary = outstanding
            .GroupBy(x => x.Currency)
            .Select(g => new TenantOutstandingCurrencySummaryDto(
                Currency: g.Key,
                OutstandingAmount: g.Sum(x => x.OutstandingAmount),
                OutstandingInvoiceCount: g.Count()))
            .OrderByDescending(s => s.OutstandingAmount)
            .ToList();

        return new TenantBillingResolutionDto(
            SubscriptionStatus: subscription?.Status.ToString(),
            OutstandingSummary: summary,
            OutstandingInvoices: invoices);
    }
}

using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.ListSubscriptionInvoices;

public sealed class ListSubscriptionInvoicesQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ITenantRepository tenants,
    IClock clock
) : IRequestHandler<ListSubscriptionInvoicesQuery, PagedResult<PlatformSubscriptionInvoiceListItemDto>>
{
    public async ValueTask<PagedResult<PlatformSubscriptionInvoiceListItemDto>> Handle(
        ListSubscriptionInvoicesQuery query, CancellationToken cancellationToken)
    {
        SubscriptionInvoiceStatus? status =
            !string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse(query.Status, out SubscriptionInvoiceStatus parsed)
                ? parsed
                : null;

        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        PagedResult<SubscriptionInvoice> page = await subscriptionInvoices.SearchAsync(
            query.Page, query.PageSize, query.OrganizationId, status, query.OverdueOnly,
            query.DueDateFrom, query.DueDateTo, today, cancellationToken);

        Dictionary<Guid, string> names = await tenants.GetNamesByIdsAsync(
            page.Items.Select(x => x.TenantId).Distinct().ToList(), cancellationToken);

        List<PlatformSubscriptionInvoiceListItemDto> items = page.Items
            .Select(x => new PlatformSubscriptionInvoiceListItemDto(
                InvoiceId: x.Id.Value,
                TenantId: x.TenantId,
                OrganizationName: names.GetValueOrDefault(x.TenantId, "Unknown"),
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

        return new PagedResult<PlatformSubscriptionInvoiceListItemDto>(items, page.Page, page.PageSize, page.Total);
    }
}

using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOutstandingDues;

/// <summary>
/// Aggregates every currently-collectible <see cref="SubscriptionInvoice"/> (Issued,
/// OutstandingAmount &gt; 0) by (TenantId, Currency) — never summing across currencies for one
/// organization (ADR-034 Task 14D §"Organization Aggregation"). The underlying row set is bounded to
/// what is actually still owed, the same small-catalogue-scale precedent
/// <see cref="Queries.GetOrganizationSubscription.GetOrganizationSubscriptionQueryHandler"/> already
/// established for Platform reads, so aggregation/pagination happens in memory rather than via a
/// database GROUP BY.
/// </summary>
public sealed class GetOutstandingDuesQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ITenantRepository tenants,
    IClock clock
) : IRequestHandler<GetOutstandingDuesQuery, PagedResult<PlatformOrganizationOutstandingDto>>
{
    public async ValueTask<PagedResult<PlatformOrganizationOutstandingDto>> Handle(
        GetOutstandingDuesQuery query, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        IReadOnlyList<SubscriptionInvoice> outstanding =
            await subscriptionInvoices.GetOutstandingAsync(query.OrganizationId, cancellationToken);

        List<(Guid TenantId, string Currency, decimal OutstandingAmount, int Count, DateOnly OldestDueDate, int? MaxDaysOverdue)> groups =
            outstanding
                .GroupBy(x => (x.TenantId, x.Currency))
                .Select(g =>
                {
                    List<int> overdueDays = g
                        .Select(x => SubscriptionInvoiceOverdueCalculator.DaysOverdue(x, today))
                        .Where(d => d is not null)
                        .Select(d => d!.Value)
                        .ToList();

                    return (
                        TenantId: g.Key.TenantId,
                        Currency: g.Key.Currency,
                        OutstandingAmount: g.Sum(x => x.OutstandingAmount),
                        Count: g.Count(),
                        OldestDueDate: g.Min(x => x.DueDate),
                        MaxDaysOverdue: overdueDays.Count > 0 ? overdueDays.Max() : (int?)null);
                })
                .OrderByDescending(g => g.MaxDaysOverdue ?? 0)
                .ThenByDescending(g => g.OutstandingAmount)
                .ToList();

        long total = groups.Count;
        List<(Guid TenantId, string Currency, decimal OutstandingAmount, int Count, DateOnly OldestDueDate, int? MaxDaysOverdue)> page = groups
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        Dictionary<Guid, string> names = await tenants.GetNamesByIdsAsync(
            page.Select(g => g.TenantId).Distinct().ToList(), cancellationToken);

        List<PlatformOrganizationOutstandingDto> items = page
            .Select(g => new PlatformOrganizationOutstandingDto(
                TenantId: g.TenantId,
                OrganizationName: names.GetValueOrDefault(g.TenantId, "Unknown"),
                Currency: g.Currency,
                OutstandingAmount: g.OutstandingAmount,
                OutstandingInvoiceCount: g.Count,
                OldestDueDate: g.OldestDueDate,
                MaxDaysOverdue: g.MaxDaysOverdue))
            .ToList();

        return new PagedResult<PlatformOrganizationOutstandingDto>(items, query.Page, query.PageSize, total);
    }
}

using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;

/// <summary>
/// Aggregates the three authoritative Platform Control Plane billing facts — never recalculated,
/// never re-derived from one another (ADR-034 Task 14L):
/// <list type="bullet">
/// <item>Invoiced = sum of <see cref="SubscriptionInvoice.TotalAmount"/> for invoices issued in the
/// requested range, excluding <see cref="SubscriptionInvoiceStatus.Void"/>. A voided invoice never
/// represented a valid charge (see <see cref="SubscriptionInvoice.Void"/>'s own doc comment), unlike
/// <see cref="SubscriptionInvoiceStatus.Canceled"/>, which was validly billed before being commercially
/// waived — so Canceled invoices still count as having been invoiced. This is a domain-derived reading
/// of the existing Void/Cancel distinction, not an invented accounting rule; see the Task 14L completion
/// report.</item>
/// <item>Collected = sum of <see cref="SubscriptionPayment.Amount"/> recorded in the requested range —
/// never <c>Invoiced - Outstanding</c>.</item>
/// <item>Outstanding/Overdue = the current persisted <see cref="SubscriptionInvoice.OutstandingAmount"/>
/// snapshot (via the same <see cref="ISubscriptionInvoiceRepository.GetOutstandingAsync"/> the Task 14D
/// Outstanding Dues view uses) and the shared <see cref="SubscriptionInvoiceOverdueCalculator"/> — no
/// aging buckets (ADR-034 stop-gate).</item>
/// </list>
/// Every figure is grouped by currency and never summed across currencies.
/// </summary>
public sealed class GetPlatformBillingSummaryQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPaymentRepository subscriptionPayments,
    IClock clock
) : IRequestHandler<GetPlatformBillingSummaryQuery, IReadOnlyList<PlatformBillingSummaryCurrencyDto>>
{
    public async ValueTask<IReadOnlyList<PlatformBillingSummaryCurrencyDto>> Handle(
        GetPlatformBillingSummaryQuery query, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        IReadOnlyList<SubscriptionInvoice> issued = await subscriptionInvoices.GetIssuedInRangeAsync(
            query.OrganizationId, query.DateFrom, query.DateTo, cancellationToken);
        IReadOnlyList<SubscriptionPayment> collected = await subscriptionPayments.SearchAsync(
            query.OrganizationId, query.DateFrom, query.DateTo, cancellationToken);
        IReadOnlyList<SubscriptionInvoice> outstanding =
            await subscriptionInvoices.GetOutstandingAsync(query.OrganizationId, cancellationToken);

        List<SubscriptionInvoice> invoicedEligible = issued
            .Where(x => x.Status != SubscriptionInvoiceStatus.Void)
            .ToList();

        Dictionary<string, (decimal Amount, int Count)> invoicedByCurrency = invoicedEligible
            .GroupBy(x => x.Currency)
            .ToDictionary(g => g.Key, g => (g.Sum(x => x.TotalAmount), g.Count()));

        Dictionary<string, (decimal Amount, int Count)> collectedByCurrency = collected
            .GroupBy(x => x.Currency)
            .ToDictionary(g => g.Key, g => (g.Sum(x => x.Amount), g.Count()));

        Dictionary<string, (decimal Amount, int Count)> outstandingByCurrency = outstanding
            .GroupBy(x => x.Currency)
            .ToDictionary(g => g.Key, g => (g.Sum(x => x.OutstandingAmount), g.Count()));

        List<(SubscriptionInvoice Invoice, int DaysOverdue)> overdue = outstanding
            .Select(x => (Invoice: x, DaysOverdue: SubscriptionInvoiceOverdueCalculator.DaysOverdue(x, today)))
            .Where(x => x.DaysOverdue is not null)
            .Select(x => (x.Invoice, x.DaysOverdue!.Value))
            .ToList();

        Dictionary<string, (decimal Amount, int Count, int MaxDaysOverdue)> overdueByCurrency = overdue
            .GroupBy(x => x.Invoice.Currency)
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(x => x.Invoice.OutstandingAmount), g.Count(), g.Max(x => x.DaysOverdue)));

        List<string> currencies = invoicedByCurrency.Keys
            .Concat(collectedByCurrency.Keys)
            .Concat(outstandingByCurrency.Keys)
            .Distinct()
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        return currencies
            .Select(currency =>
            {
                (decimal InvoicedAmount, int InvoicedCount) = invoicedByCurrency.GetValueOrDefault(currency);
                (decimal CollectedAmount, int CollectedCount) = collectedByCurrency.GetValueOrDefault(currency);
                (decimal OutstandingAmount, int OutstandingCount) = outstandingByCurrency.GetValueOrDefault(currency);
                bool hasOverdue = overdueByCurrency.TryGetValue(
                    currency, out (decimal Amount, int Count, int MaxDaysOverdue) overdueForCurrency);

                return new PlatformBillingSummaryCurrencyDto(
                    Currency: currency,
                    InvoicedAmount: InvoicedAmount,
                    InvoicedInvoiceCount: InvoicedCount,
                    CollectedAmount: CollectedAmount,
                    CollectedPaymentCount: CollectedCount,
                    OutstandingAmount: OutstandingAmount,
                    OutstandingInvoiceCount: OutstandingCount,
                    OverdueOutstandingAmount: hasOverdue ? overdueForCurrency.Amount : 0m,
                    OverdueInvoiceCount: hasOverdue ? overdueForCurrency.Count : 0,
                    MaxDaysOverdue: hasOverdue ? overdueForCurrency.MaxDaysOverdue : null);
            })
            .ToList();
    }
}

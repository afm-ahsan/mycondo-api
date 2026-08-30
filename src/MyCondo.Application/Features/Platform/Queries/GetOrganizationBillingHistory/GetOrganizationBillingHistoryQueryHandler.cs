using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;

/// <summary>
/// Merges one organization's full invoice and payment record sets into a single date-ordered timeline
/// (ADR-034 Task 14L) — invoice numbers for payment events resolve from that same full invoice set
/// (loaded unfiltered by date) so a payment recorded inside the requested range always displays the
/// invoice it settled, even when that invoice's own <c>IssueDate</c> falls outside the range. Pagination
/// happens in memory over the merged list, the same small-catalogue-scale precedent
/// <see cref="Queries.GetOutstandingDues.GetOutstandingDuesQueryHandler"/> already established.
/// </summary>
public sealed class GetOrganizationBillingHistoryQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPaymentRepository subscriptionPayments,
    IClock clock
) : IRequestHandler<GetOrganizationBillingHistoryQuery, PagedResult<OrganizationBillingHistoryEventDto>>
{
    public async ValueTask<PagedResult<OrganizationBillingHistoryEventDto>> Handle(
        GetOrganizationBillingHistoryQuery query, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        IReadOnlyList<SubscriptionInvoice> invoices = await subscriptionInvoices.GetIssuedInRangeAsync(
            query.OrganizationId, null, null, cancellationToken);
        IReadOnlyList<SubscriptionPayment> payments = await subscriptionPayments.SearchAsync(
            query.OrganizationId, null, null, cancellationToken);

        Dictionary<SubscriptionInvoiceId, SubscriptionInvoice> invoicesById = invoices.ToDictionary(x => x.Id);

        IEnumerable<OrganizationBillingHistoryEventDto> invoiceEvents = invoices
            .Where(x => (query.DateFrom is null || x.IssueDate >= query.DateFrom.Value)
                && (query.DateTo is null || x.IssueDate <= query.DateTo.Value))
            .Select(x => new OrganizationBillingHistoryEventDto(
                EventDate: x.IssueDate,
                EventType: "InvoiceIssued",
                InvoiceId: x.Id.Value,
                InvoiceNumber: x.InvoiceNumber,
                Currency: x.Currency,
                Amount: x.TotalAmount,
                InvoiceStatus: x.Status.ToString(),
                OutstandingAmount: x.OutstandingAmount,
                DaysOverdue: SubscriptionInvoiceOverdueCalculator.DaysOverdue(x, today),
                PaymentId: null,
                ReferenceNumber: null));

        IEnumerable<OrganizationBillingHistoryEventDto> paymentEvents = payments
            .Where(x => (query.DateFrom is null || x.PaymentDate >= query.DateFrom.Value)
                && (query.DateTo is null || x.PaymentDate <= query.DateTo.Value))
            .Select(x =>
            {
                invoicesById.TryGetValue(x.SubscriptionInvoiceId, out SubscriptionInvoice? invoice);
                return new OrganizationBillingHistoryEventDto(
                    EventDate: x.PaymentDate,
                    EventType: "PaymentRecorded",
                    InvoiceId: x.SubscriptionInvoiceId.Value,
                    InvoiceNumber: invoice?.InvoiceNumber ?? "Unknown",
                    Currency: x.Currency,
                    Amount: x.Amount,
                    InvoiceStatus: null,
                    OutstandingAmount: null,
                    DaysOverdue: null,
                    PaymentId: x.Id.Value,
                    ReferenceNumber: x.ReferenceNumber);
            });

        List<OrganizationBillingHistoryEventDto> events = invoiceEvents
            .Concat(paymentEvents)
            .OrderByDescending(x => x.EventDate)
            .ThenBy(x => x.EventType, StringComparer.Ordinal)
            .ToList();

        long total = events.Count;
        List<OrganizationBillingHistoryEventDto> page = events
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<OrganizationBillingHistoryEventDto>(page, query.Page, query.PageSize, total);
    }
}

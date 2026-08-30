using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

public interface ISubscriptionInvoiceRepository
{
    Task<SubscriptionInvoice?> GetByIdAsync(SubscriptionInvoiceId id, CancellationToken cancellationToken);

    /// <summary>All invoices ever issued for one organization subscription, newest billing period
    /// first — including terminal (Void/Canceled) rows. Used by later invoice-generation/history slices
    /// to check what has already been billed for a subscription.</summary>
    Task<IReadOnlyList<SubscriptionInvoice>> GetForOrganizationSubscriptionAsync(
        OrganizationSubscriptionId organizationSubscriptionId, CancellationToken cancellationToken);

    /// <summary>Platform-scope, paginated invoice search (ADR-034 Task 14D) — optionally filtered by
    /// organization, status, and/or due-date range. <paramref name="overdueOnly"/> restricts to invoices
    /// that are <see cref="SubscriptionInvoiceStatus.Issued"/>, still carry a positive
    /// <see cref="SubscriptionInvoice.OutstandingAmount"/>, and whose <see cref="SubscriptionInvoice.DueDate"/>
    /// is strictly before <paramref name="today"/> (the caller's already-resolved Dhaka business date —
    /// this repository never derives "today" itself).</summary>
    Task<PagedResult<SubscriptionInvoice>> SearchAsync(
        int page,
        int pageSize,
        Guid? tenantId,
        SubscriptionInvoiceStatus? status,
        bool? overdueOnly,
        DateOnly? dueDateFrom,
        DateOnly? dueDateTo,
        DateOnly today,
        CancellationToken cancellationToken);

    /// <summary>Every currently-collectible invoice (Issued, OutstandingAmount &gt; 0), optionally
    /// scoped to one organization — the bounded read set the Task 14D outstanding-dues/collections view
    /// aggregates over. Never includes Paid/Void/Canceled rows.</summary>
    Task<IReadOnlyList<SubscriptionInvoice>> GetOutstandingAsync(Guid? tenantId, CancellationToken cancellationToken);

    /// <summary>Invoices issued within [<paramref name="issueDateFrom"/>, <paramref name="issueDateTo"/>]
    /// (either bound optional), optionally scoped to one organization — every status is included
    /// (Issued/Paid/Void/Canceled); the caller decides which statuses count toward a KPI (ADR-034 Task
    /// 14L, see <c>GetPlatformBillingSummaryQueryHandler</c>). Also the invoice side of
    /// <c>GetOrganizationBillingHistoryQueryHandler</c>'s merged invoice/payment timeline.</summary>
    Task<IReadOnlyList<SubscriptionInvoice>> GetIssuedInRangeAsync(
        Guid? tenantId, DateOnly? issueDateFrom, DateOnly? issueDateTo, CancellationToken cancellationToken);

    /// <summary>The charge line(s) for one invoice, oldest first — <see cref="SubscriptionInvoiceLine"/>
    /// has no navigation collection on <see cref="SubscriptionInvoice"/> itself (see that type's doc
    /// comment), so invoice-detail reads fetch lines separately through this method.</summary>
    Task<IReadOnlyList<SubscriptionInvoiceLine>> GetLinesAsync(
        SubscriptionInvoiceId subscriptionInvoiceId, CancellationToken cancellationToken);

    void Add(SubscriptionInvoice invoice);

    void AddLines(IEnumerable<SubscriptionInvoiceLine> lines);
}

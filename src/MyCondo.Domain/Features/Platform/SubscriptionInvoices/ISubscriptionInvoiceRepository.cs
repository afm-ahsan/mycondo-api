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

    void Add(SubscriptionInvoice invoice);

    void AddLines(IEnumerable<SubscriptionInvoiceLine> lines);
}

using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Domain.Features.Platform.SubscriptionPayments;

public interface ISubscriptionPaymentRepository
{
    Task<SubscriptionPayment?> GetByIdAsync(SubscriptionPaymentId id, CancellationToken cancellationToken);

    /// <summary>Application-level pre-check for the same duplicate-reference guard the
    /// <c>ux_subscription_payments_tenant_id_reference_number</c> database constraint enforces durably —
    /// mirrors <see cref="SubscriptionInvoices.ISubscriptionInvoiceRepository.GetForOrganizationSubscriptionAsync"/>'s
    /// role as Task 14B's own pre-check before the DB backstop.</summary>
    Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken cancellationToken);

    /// <summary>Payment history for one invoice, oldest first (ADR-034 Task 14D read model) — display
    /// only; the invoice's own <see cref="SubscriptionInvoices.SubscriptionInvoice.OutstandingAmount"/>
    /// remains the sole balance authority, never re-derived from this list.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> GetForInvoiceAsync(
        SubscriptionInvoiceId subscriptionInvoiceId, CancellationToken cancellationToken);

    void Add(SubscriptionPayment payment);
}

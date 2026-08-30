namespace MyCondo.Domain.Features.Platform.SubscriptionPayments;

public interface ISubscriptionPaymentRepository
{
    Task<SubscriptionPayment?> GetByIdAsync(SubscriptionPaymentId id, CancellationToken cancellationToken);

    /// <summary>Application-level pre-check for the same duplicate-reference guard the
    /// <c>ux_subscription_payments_tenant_id_reference_number</c> database constraint enforces durably —
    /// mirrors <see cref="SubscriptionInvoices.ISubscriptionInvoiceRepository.GetForOrganizationSubscriptionAsync"/>'s
    /// role as Task 14B's own pre-check before the DB backstop.</summary>
    Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken cancellationToken);

    void Add(SubscriptionPayment payment);
}

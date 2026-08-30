using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Domain.Features.Platform.SubscriptionPayments;

/// <summary>
/// A manually-recorded settlement against one Platform <see cref="SubscriptionInvoice"/> (ADR-034 Task
/// 14C) — the minimal Platform Control Plane billing record, structurally separate from tenant
/// condominium Finance (no <c>ChartOfAccounts</c>/<c>LedgerPosting</c>/<c>FinancialAccount</c>/
/// <c>ResidentLedger</c>/tenant <c>Payment</c> involvement of any kind). Lives in the <c>platform</c>
/// schema, no FK to <c>tenancy.tenants</c> or to <see cref="SubscriptionInvoice"/> — same no-FK
/// convention <see cref="SubscriptionInvoice"/> itself already uses for its own cross-aggregate
/// references.
///
/// <para><b>Immutable provenance:</b> once recorded, a payment is never mutated or deleted — there is
/// no reversal/void operation in this task (ADR-034 Task 14C stop-gates). <see cref="ReferenceNumber"/>
/// is mandatory (unlike tenant <c>Payment.ReferenceNumber</c>, which is optional) specifically so a
/// durable database uniqueness constraint on (TenantId, ReferenceNumber) can serve as this record's
/// idempotency backstop — see <c>SubscriptionPaymentConfiguration</c> and
/// <c>MyCondoDbContext.SaveChangesAsync</c>.</para>
///
/// <para>This entity carries no invoice-outstanding-balance logic of its own —
/// <see cref="SubscriptionInvoice.ApplyPayment"/> owns that invariant on the invoice aggregate itself,
/// the same separation <see cref="SubscriptionInvoice"/>/<see cref="SubscriptionInvoiceLine"/> already
/// establish between the invoice and its charge lines.</para>
/// </summary>
public sealed class SubscriptionPayment : AggregateRoot<SubscriptionPaymentId>
{
    public Guid TenantId { get; private set; }
    public SubscriptionInvoiceId SubscriptionInvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    private SubscriptionPayment()
    {
        Currency = null!;
        ReferenceNumber = null!;
    }

    private SubscriptionPayment(
        SubscriptionPaymentId id,
        Guid tenantId,
        SubscriptionInvoiceId subscriptionInvoiceId,
        decimal amount,
        string currency,
        DateOnly paymentDate,
        string referenceNumber,
        string? notes,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        SubscriptionInvoiceId = subscriptionInvoiceId;
        Amount = amount;
        Currency = currency;
        PaymentDate = paymentDate;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        RecordedAtUtc = nowUtc;
    }

    /// <summary>
    /// Materializes the payment record. Does not itself touch the invoice's outstanding balance —
    /// callers apply <see cref="SubscriptionInvoice.ApplyPayment"/> to the invoice aggregate and record
    /// this alongside it in the same unit of work (see <c>RecordSubscriptionPaymentCommandHandler</c>).
    /// </summary>
    public static SubscriptionPayment Record(
        Guid tenantId,
        SubscriptionInvoiceId subscriptionInvoiceId,
        decimal amount,
        string currency,
        DateOnly paymentDate,
        string referenceNumber,
        string? notes,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceNumber);

        return new SubscriptionPayment(
            SubscriptionPaymentId.New(), tenantId, subscriptionInvoiceId, amount, currency.Trim(),
            paymentDate, referenceNumber.Trim(), string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), nowUtc);
    }
}

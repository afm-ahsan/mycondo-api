using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

/// <summary>
/// A manually-billed SaaS subscription charge for one organization/billing period (ADR-034 §2) — the
/// minimal Platform Control Plane billing aggregate, structurally separate from tenant condominium
/// Finance (no <c>ChartOfAccounts</c>/<c>LedgerPosting</c>/<c>FinancialAccount</c>/<c>ResidentLedger</c>
/// involvement of any kind). Lives in the <c>platform</c> schema, no FK to <c>tenancy.tenants</c> or to
/// <see cref="OrganizationSubscriptions.OrganizationSubscription"/> — same no-FK convention as
/// <c>OrganizationSubscription.TenantId</c>/<c>PlatformAuditLogEntry.TenantId</c>.
///
/// <para><b>Charge provenance:</b> the invoice carries no charge amount of its own beyond
/// <see cref="TotalAmount"/> (the sum of its <see cref="SubscriptionInvoiceLine"/>s) — package/version/
/// commercial-snapshot provenance for the charge lives on the line, mirroring how tenant
/// <c>Invoice</c>/<c>InvoiceLine</c> separate the two.</para>
///
/// <para><b>Lifecycle</b> is intentionally the smallest model appropriate for manual SaaS billing:
/// <see cref="SubscriptionInvoiceStatus.Issued"/> is the only state <see cref="Issue"/> can produce,
/// and <see cref="MarkPaid"/>/<see cref="Void"/>/<see cref="Cancel"/> are mutually exclusive terminal
/// transitions out of it — no accounting posting states, no partial-payment tracking (that belongs to a
/// later Task 14 slice once payment recording exists).</para>
/// </summary>
public sealed class SubscriptionInvoice : AggregateRoot<SubscriptionInvoiceId>
{
    public Guid TenantId { get; private set; }
    public OrganizationSubscriptionId OrganizationSubscriptionId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public DateOnly BillingPeriodStart { get; private set; }
    public DateOnly BillingPeriodEnd { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Currency { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public SubscriptionInvoiceStatus Status { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTimeOffset? CanceledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }

    private SubscriptionInvoice()
    {
        InvoiceNumber = null!;
        Currency = null!;
    }

    private SubscriptionInvoice(
        SubscriptionInvoiceId id,
        Guid tenantId,
        OrganizationSubscriptionId organizationSubscriptionId,
        string invoiceNumber,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        decimal totalAmount,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OrganizationSubscriptionId = organizationSubscriptionId;
        InvoiceNumber = invoiceNumber;
        BillingPeriodStart = billingPeriodStart;
        BillingPeriodEnd = billingPeriodEnd;
        IssueDate = issueDate;
        DueDate = dueDate;
        Currency = currency;
        TotalAmount = totalAmount;
        OutstandingAmount = totalAmount;
        Status = SubscriptionInvoiceStatus.Issued;
        IssuedAtUtc = nowUtc;
    }

    /// <summary>
    /// Validates the line set and materializes the invoice plus its line(s). <see cref="TotalAmount"/>
    /// is always the sum of <paramref name="lines"/>' <c>LineAmount</c> — never an independent
    /// caller-supplied value — mirroring <c>Invoice.Issue</c>'s own subtotal derivation. No workflow
    /// (duplicate-period checks, invoice numbering) is implemented here; that belongs to the
    /// invoice-generation command this task's domain foundation exists to support.
    /// </summary>
    public static (SubscriptionInvoice Invoice, IReadOnlyList<SubscriptionInvoiceLine> Lines) Issue(
        Guid tenantId,
        OrganizationSubscriptionId organizationSubscriptionId,
        string invoiceNumber,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        IReadOnlyList<SubscriptionInvoiceLineInput> lines,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (billingPeriodEnd < billingPeriodStart)
        {
            throw new ArgumentException("BillingPeriodEnd must not be before BillingPeriodStart.", nameof(billingPeriodEnd));
        }

        if (dueDate < issueDate)
        {
            throw new ArgumentException("DueDate must not be before IssueDate.", nameof(dueDate));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("A subscription invoice needs at least one charge line.", nameof(lines));
        }

        foreach (SubscriptionInvoiceLineInput line in lines)
        {
            if (line.LineAmount <= 0)
            {
                throw new ArgumentException("Every subscription invoice line amount must be positive.", nameof(lines));
            }
        }

        decimal total = lines.Sum(l => l.LineAmount);

        SubscriptionInvoiceId invoiceId = SubscriptionInvoiceId.New();
        SubscriptionInvoice invoice = new(
            invoiceId, tenantId, organizationSubscriptionId, invoiceNumber.Trim(), billingPeriodStart,
            billingPeriodEnd, issueDate, dueDate, currency.Trim(), total, nowUtc);

        List<SubscriptionInvoiceLine> invoiceLines = lines
            .Select(input => new SubscriptionInvoiceLine(SubscriptionInvoiceLineId.New(), invoiceId, input, nowUtc))
            .ToList();

        return (invoice, invoiceLines);
    }

    /// <summary>Issued → Paid — the full charge has been settled outside this domain (no partial-payment
    /// tracking at this foundation stage; see the type's own doc comment).</summary>
    public void MarkPaid(DateTimeOffset paidAtUtc)
    {
        if (Status != SubscriptionInvoiceStatus.Issued)
        {
            throw new SubscriptionInvoiceInvalidTransitionException(Id, Status, SubscriptionInvoiceStatus.Paid);
        }

        Status = SubscriptionInvoiceStatus.Paid;
        OutstandingAmount = 0m;
        PaidAtUtc = paidAtUtc;
    }

    /// <summary>Issued → Void — the invoice itself was wrong (e.g. billed against the wrong
    /// subscription/period) and never represented a valid charge.</summary>
    public void Void(string reason, DateTimeOffset voidedAtUtc)
    {
        if (Status != SubscriptionInvoiceStatus.Issued)
        {
            throw new SubscriptionInvoiceInvalidTransitionException(Id, Status, SubscriptionInvoiceStatus.Void);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = SubscriptionInvoiceStatus.Void;
        OutstandingAmount = 0m;
        VoidedAtUtc = voidedAtUtc;
        VoidReason = reason.Trim();
    }

    /// <summary>Issued → Canceled — the charge was valid when issued but is being called off before
    /// payment (e.g. a commercial waiver), distinct from <see cref="Void"/>'s "this was never a valid
    /// charge" correction.</summary>
    public void Cancel(string reason, DateTimeOffset canceledAtUtc)
    {
        if (Status != SubscriptionInvoiceStatus.Issued)
        {
            throw new SubscriptionInvoiceInvalidTransitionException(Id, Status, SubscriptionInvoiceStatus.Canceled);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = SubscriptionInvoiceStatus.Canceled;
        OutstandingAmount = 0m;
        CanceledAtUtc = canceledAtUtc;
        CancelReason = reason.Trim();
    }
}

using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// One billing-period invoice for one flat, carrying one <see cref="InvoiceLine"/> per applicable
/// <see cref="ServiceChargeRules.ServiceChargeRule"/>. <see cref="Issue"/> is the only way to
/// produce one — batch generation calls it once per (flat, period) after computing lines via the
/// Application-layer <c>ServiceChargeCalculator</c>. Posted immutable once issued: <see cref="Void"/>
/// changes status/void metadata only, never <see cref="InvoiceNumber"/> or the lines themselves.
/// </summary>
public sealed class Invoice : AggregateRoot<InvoiceId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public FlatId FlatId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public InvoiceSource Source { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public LedgerPostingId LedgerPostingId { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public LedgerPostingId? VoidLedgerPostingId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Derived, never stored — no drift risk between this and <see cref="AmountPaid"/>.</summary>
    public decimal Balance => TotalAmount - AmountPaid;

    private Invoice()
    {
        InvoiceNumber = null!;
    }

    private Invoice(
        InvoiceId id,
        Guid tenantId,
        BuildingId buildingId,
        FlatId flatId,
        string invoiceNumber,
        InvoiceSource source,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly invoiceDate,
        DateOnly dueDate,
        decimal subtotalAmount,
        LedgerPostingId ledgerPostingId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        FlatId = flatId;
        InvoiceNumber = invoiceNumber;
        Source = source;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        SubtotalAmount = subtotalAmount;
        TotalAmount = subtotalAmount;
        AmountPaid = 0m;
        Status = InvoiceStatus.Issued;
        LedgerPostingId = ledgerPostingId;
        IssuedAtUtc = nowUtc;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Validates the line set and materializes the invoice plus its lines. No discount/
    /// adjustment support this slice — <see cref="TotalAmount"/> always equals
    /// <see cref="SubtotalAmount"/>, kept as a separate field for forward compatibility rather than
    /// computed as an alias. <paramref name="dueDate"/> is expected to be <paramref name="periodEnd"/>
    /// (late-fee/grace-period policy is unresolved — see Slice E's final report — so no other default
    /// is invented here).</summary>
    public static (Invoice Invoice, IReadOnlyList<InvoiceLine> Lines) Issue(
        Guid tenantId,
        BuildingId buildingId,
        FlatId flatId,
        string invoiceNumber,
        InvoiceSource source,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly invoiceDate,
        DateOnly dueDate,
        IReadOnlyList<InvoiceLineInput> lines,
        LedgerPostingId ledgerPostingId,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("An invoice needs at least one line.", nameof(lines));
        }

        foreach (InvoiceLineInput line in lines)
        {
            if (line.LineAmount <= 0)
            {
                throw new ArgumentException("Every invoice line amount must be positive.", nameof(lines));
            }
        }

        decimal subtotal = lines.Sum(l => l.LineAmount);

        InvoiceId invoiceId = InvoiceId.New();
        Invoice invoice = new(
            invoiceId, tenantId, buildingId, flatId, invoiceNumber, source, periodStart, periodEnd, invoiceDate,
            dueDate, subtotal, ledgerPostingId, nowUtc);

        List<InvoiceLine> invoiceLines = lines
            .Select(input => new InvoiceLine(InvoiceLineId.New(), tenantId, invoiceId, input, nowUtc))
            .ToList();

        return (invoice, invoiceLines);
    }

    /// <summary>Applies part of a FIFO payment allocation to this invoice. Caller (payment handler)
    /// is responsible for capping <paramref name="amount"/> at <see cref="Balance"/> before calling —
    /// this is a defensive backstop, not the primary guard.</summary>
    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Allocated amount must be positive.");
        }

        if (amount > Balance)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Allocated amount cannot exceed the invoice's outstanding balance.");
        }

        AmountPaid += amount;
        Status = AmountPaid >= TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        Version++;
    }

    /// <summary>Reverses part of a previously-applied FIFO allocation — called when the underlying
    /// Payment is reversed. Symmetric with <see cref="ApplyPayment"/>: recomputes <see cref="Status"/>
    /// from the new <see cref="AmountPaid"/>. The <see cref="Void"/> guard (<c>AmountPaid == 0</c>)
    /// makes a voided invoice structurally unreachable here — it could never have had a payment
    /// applied to it in the first place — so that check is defensive, not a reachable path today.
    /// </summary>
    public void ReverseAppliedPayment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Reversed amount must be positive.");
        }

        if (amount > AmountPaid)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Reversed amount cannot exceed the invoice's amount paid.");
        }

        if (Status == InvoiceStatus.Void)
        {
            throw new InvoiceAlreadyVoidException(Id);
        }

        AmountPaid -= amount;
        Status = AmountPaid <= 0 ? InvoiceStatus.Issued : InvoiceStatus.PartiallyPaid;
        Version++;
    }

    /// <summary>Restricted to invoices with <c>AmountPaid == 0</c> — see
    /// <see cref="InvoiceCannotBeVoidedException"/>. <paramref name="voidLedgerPostingId"/> is the
    /// reversing posting the caller already created in the same transaction as this call.</summary>
    public void Void(string reason, Guid? voidedBy, LedgerPostingId voidLedgerPostingId, DateTimeOffset nowUtc)
    {
        if (Status == InvoiceStatus.Void)
        {
            throw new InvoiceAlreadyVoidException(Id);
        }

        if (AmountPaid > 0)
        {
            throw new InvoiceCannotBeVoidedException(Id);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = InvoiceStatus.Void;
        VoidedAtUtc = nowUtc;
        VoidedBy = voidedBy;
        VoidReason = reason.Trim();
        VoidLedgerPostingId = voidLedgerPostingId;
        Version++;
    }
}

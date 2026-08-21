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
    public decimal WaivedAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public LedgerPostingId LedgerPostingId { get; private set; }
    /// <summary>Which tenant-wide income account this invoice's issuance posting credited — recorded
    /// at issuance so <c>VoidInvoiceCommandHandler</c> can reverse against the exact same account
    /// without re-deriving it from <see cref="Source"/> (which alone cannot distinguish, e.g., Gas from
    /// Electricity utility invoices). See ADR-027's Billing↔Finance integration.</summary>
    public LedgerAccountType IncomeAccountType { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public LedgerPostingId? VoidLedgerPostingId { get; private set; }
    public DateTimeOffset? WaivedAtUtc { get; private set; }
    public Guid? WaivedBy { get; private set; }
    public string? WaiveReason { get; private set; }
    public LedgerPostingId? WaiveLedgerPostingId { get; private set; }
    /// <summary>Historical responsible-party snapshot (Owner/Tenant + the resident/ownership/occupancy
    /// identifiers behind them) as of <see cref="IssuedAtUtc"/> — see
    /// <see cref="ResponsiblePartySnapshot"/>. Null when neither an active owner nor an active tenant
    /// could be resolved for the flat at issuance time; never recomputed afterward.</summary>
    public ResponsiblePartyType? ResponsiblePartyType { get; private set; }
    public Guid? ResponsibleResidentId { get; private set; }
    public Guid? ResponsibleFlatOwnershipId { get; private set; }
    public Guid? ResponsibleOccupancyRegistrationId { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Derived, never stored — no drift risk against <see cref="AmountPaid"/>/
    /// <see cref="WaivedAmount"/>.</summary>
    public decimal Balance => TotalAmount - AmountPaid - WaivedAmount;

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
        LedgerAccountType incomeAccountType,
        ResponsiblePartySnapshot? responsibleParty,
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
        WaivedAmount = 0m;
        Status = InvoiceStatus.Issued;
        LedgerPostingId = ledgerPostingId;
        IncomeAccountType = incomeAccountType;
        ResponsiblePartyType = responsibleParty?.PartyType;
        ResponsibleResidentId = responsibleParty?.ResponsibleResidentId;
        ResponsibleFlatOwnershipId = responsibleParty?.FlatOwnershipId;
        ResponsibleOccupancyRegistrationId = responsibleParty?.OccupancyRegistrationId;
        IssuedAtUtc = nowUtc;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    /// <summary>Validates the line set and materializes the invoice plus its lines. No discount/
    /// adjustment support this slice — <see cref="TotalAmount"/> always equals
    /// <see cref="SubtotalAmount"/>, kept as a separate field for forward compatibility rather than
    /// computed as an alias. <paramref name="dueDate"/> is expected to be <paramref name="periodEnd"/>
    /// (late-fee/grace-period policy is unresolved — see Slice E's final report — so no other default
    /// is invented here). <paramref name="incomeAccountType"/> is the tenant-wide income account the
    /// caller already credited in the same posting — recorded so <see cref="Void"/>'s reversal can
    /// reuse it directly. <paramref name="responsibleParty"/> is an optional historical snapshot; see
    /// <see cref="ResponsiblePartySnapshot"/>.</summary>
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
        LedgerAccountType incomeAccountType,
        ResponsiblePartySnapshot? responsibleParty,
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
            dueDate, subtotal, ledgerPostingId, incomeAccountType, responsibleParty, nowUtc);

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
        RecomputeStatus();
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
        RecomputeStatus();
        Version++;
    }

    /// <summary>Writes off part or all of the outstanding balance without any cash changing hands —
    /// e.g. a fine waiver. One-time only per invoice (<see cref="WaivedAmount"/> must be zero going
    /// in): a second waiver attempt on an already-waived invoice is rejected rather than silently
    /// accumulating, which keeps this call idempotent-by-construction alongside the caller's ledger
    /// posting (same invoice id used as the posting's SourceId). <paramref name="waiveLedgerPostingId"/>
    /// is the Dr AdjustmentsAndWaivers / Cr ResidentReceivable posting the caller already created in
    /// the same transaction as this call.</summary>
    public void Waive(decimal amount, string reason, Guid? waivedBy, LedgerPostingId waiveLedgerPostingId, DateTimeOffset nowUtc)
    {
        if (Status == InvoiceStatus.Void)
        {
            throw new InvoiceAlreadyVoidException(Id);
        }

        if (WaivedAmount > 0)
        {
            throw new InvoiceAlreadyWaivedException(Id);
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Waived amount must be positive.");
        }

        if (amount > Balance)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Waived amount cannot exceed the invoice's outstanding balance.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        WaivedAmount = amount;
        WaivedAtUtc = nowUtc;
        WaivedBy = waivedBy;
        WaiveReason = reason.Trim();
        WaiveLedgerPostingId = waiveLedgerPostingId;
        RecomputeStatus();
        Version++;
    }

    /// <summary>Restricted to invoices with <c>AmountPaid == 0</c> and <c>WaivedAmount == 0</c> — see
    /// <see cref="InvoiceCannotBeVoidedException"/>. <paramref name="voidLedgerPostingId"/> is the
    /// reversing posting the caller already created in the same transaction as this call.</summary>
    public void Void(string reason, Guid? voidedBy, LedgerPostingId voidLedgerPostingId, DateTimeOffset nowUtc)
    {
        if (Status == InvoiceStatus.Void)
        {
            throw new InvoiceAlreadyVoidException(Id);
        }

        if (AmountPaid > 0 || WaivedAmount > 0)
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

    /// <summary>Single source of truth for deriving <see cref="Status"/> from the current
    /// (<see cref="AmountPaid"/>, <see cref="WaivedAmount"/>, <see cref="TotalAmount"/>) triple —
    /// shared by <see cref="ApplyPayment"/>, <see cref="ReverseAppliedPayment"/>, and
    /// <see cref="Waive"/> so the precedence rule lives in exactly one place. A waiver "wins" the
    /// label whenever one has occurred (fully settled + ever waived → Waived, not Paid; still
    /// outstanding + ever waived → PartiallyWaived, not PartiallyPaid) since the waiver is the more
    /// operationally significant fact for a Fine.</summary>
    private void RecomputeStatus()
    {
        if (Balance <= 0)
        {
            Status = WaivedAmount > 0 && AmountPaid <= 0 ? InvoiceStatus.Waived : InvoiceStatus.Paid;
        }
        else
        {
            Status = WaivedAmount > 0
                ? InvoiceStatus.PartiallyWaived
                : (AmountPaid > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Issued);
        }
    }
}

using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>
/// One period's (typically a month's) interest earned on a <see cref="FixedDeposit"/> — "Dr Interest
/// Receivable / Cr FD Interest Income" (Template 4's accrual-mode default). A distinct aggregate root
/// (not a value object on <see cref="FixedDeposit"/>) because each accrual event needs its own id to
/// serve as the <c>IFinancialPostingService</c> posting <c>SourceId</c> — reusing
/// <see cref="FixedDepositId"/> across multiple periods under the same <c>PostingPurpose</c> would
/// collide with the duplicate-posting idempotency guard (see <c>FinancialPostingService.PostAsync</c>).
/// </summary>
public sealed class FixedDepositInterestAccrual : AggregateRoot<FixedDepositInterestAccrualId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FixedDepositId FixedDepositId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly AccountingDate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string? Notes { get; private set; }
    public LedgerPostingId PostingId { get; private set; }
    public LedgerPostingId? ReversalPostingId { get; private set; }
    public bool IsReversed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FixedDepositInterestAccrual()
    {
    }

    private FixedDepositInterestAccrual(
        FixedDepositInterestAccrualId id, Guid tenantId, FixedDepositId fixedDepositId, DateOnly periodStart,
        DateOnly periodEnd, DateOnly accountingDate, decimal grossAmount, string? notes,
        LedgerPostingId postingId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FixedDepositId = fixedDepositId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        AccountingDate = accountingDate;
        GrossAmount = grossAmount;
        Notes = notes;
        PostingId = postingId;
        CreatedAtUtc = nowUtc;
    }

    public static FixedDepositInterestAccrual Record(
        FixedDepositInterestAccrualId id, Guid tenantId, FixedDepositId fixedDepositId, DateOnly periodStart,
        DateOnly periodEnd, DateOnly accountingDate, decimal grossAmount, string? notes,
        LedgerPostingId postingId, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (grossAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grossAmount), grossAmount, "Amount must be greater than zero.");
        }

        if (periodEnd < periodStart)
        {
            throw new ArgumentException("PeriodEnd cannot precede PeriodStart.", nameof(periodEnd));
        }

        return new FixedDepositInterestAccrual(
            id, tenantId, fixedDepositId, periodStart, periodEnd, accountingDate, grossAmount, notes?.Trim(),
            postingId, nowUtc);
    }

    /// <summary>Reverses an accrual entered in error — the command handler enforces that no
    /// <c>FixedDepositInterestReceipt</c> has already drawn against it before allowing this.</summary>
    public void MarkReversed(LedgerPostingId reversalPostingId, DateTimeOffset nowUtc)
    {
        if (IsReversed)
        {
            throw new InvalidOperationException($"Interest accrual {Id} is already reversed.");
        }

        IsReversed = true;
        ReversalPostingId = reversalPostingId;
        UpdatedAtUtc = nowUtc;
    }
}

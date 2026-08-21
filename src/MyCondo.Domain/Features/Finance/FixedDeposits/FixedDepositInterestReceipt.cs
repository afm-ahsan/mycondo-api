using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.Features.Finance.FixedDeposits;

/// <summary>
/// An actual interest receipt against a <see cref="FixedDeposit"/>'s accrued Interest Receivable —
/// "Dr Bank (net) [+ Dr Deduction Expense (deduction)] / Cr Interest Receivable (gross)". Gross/
/// deduction/net are all persisted per Template 4's requirement (e.g. Gross 60,000 / Deduction 6,000 /
/// Net 54,000); <see cref="NetAmount"/> is derived, never independently entered, so the three numbers
/// can never drift apart. A distinct aggregate root from <see cref="FixedDepositInterestAccrual"/> for
/// the same reason that one is distinct from <see cref="FixedDeposit"/> — its own id as the posting
/// <c>SourceId</c>, and receipts can be partial/multiple against one FD's outstanding receivable.
/// </summary>
public sealed class FixedDepositInterestReceipt : AggregateRoot<FixedDepositInterestReceiptId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FixedDepositId FixedDepositId { get; private set; }
    public DateOnly AccountingDate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DeductionAmount { get; private set; }
    public decimal NetAmount => GrossAmount - DeductionAmount;
    public FinancialAccountId ReceivingFinancialAccountId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public LedgerPostingId PostingId { get; private set; }
    public LedgerPostingId? ReversalPostingId { get; private set; }
    public bool IsReversed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FixedDepositInterestReceipt()
    {
    }

    private FixedDepositInterestReceipt(
        FixedDepositInterestReceiptId id, Guid tenantId, FixedDepositId fixedDepositId, DateOnly accountingDate,
        decimal grossAmount, decimal deductionAmount, FinancialAccountId receivingFinancialAccountId,
        string? referenceNumber, string? notes, LedgerPostingId postingId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FixedDepositId = fixedDepositId;
        AccountingDate = accountingDate;
        GrossAmount = grossAmount;
        DeductionAmount = deductionAmount;
        ReceivingFinancialAccountId = receivingFinancialAccountId;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        PostingId = postingId;
        CreatedAtUtc = nowUtc;
    }

    public static FixedDepositInterestReceipt Record(
        FixedDepositInterestReceiptId id, Guid tenantId, FixedDepositId fixedDepositId, DateOnly accountingDate,
        decimal grossAmount, decimal deductionAmount, FinancialAccountId receivingFinancialAccountId,
        string? referenceNumber, string? notes, LedgerPostingId postingId, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (grossAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grossAmount), grossAmount, "Amount must be greater than zero.");
        }

        if (deductionAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deductionAmount), deductionAmount, "Deduction cannot be negative.");
        }

        if (deductionAmount > grossAmount)
        {
            throw new ArgumentException("Deduction cannot exceed the gross interest amount.", nameof(deductionAmount));
        }

        return new FixedDepositInterestReceipt(
            id, tenantId, fixedDepositId, accountingDate, grossAmount, deductionAmount,
            receivingFinancialAccountId, referenceNumber?.Trim(), notes?.Trim(), postingId, nowUtc);
    }

    /// <summary>Reverses a receipt entered in error — reinstates the underlying accrual's outstanding
    /// receivable (the command handler posts the reversing ledger entries and calls this).</summary>
    public void MarkReversed(LedgerPostingId reversalPostingId, DateTimeOffset nowUtc)
    {
        if (IsReversed)
        {
            throw new InvalidOperationException($"Interest receipt {Id} is already reversed.");
        }

        IsReversed = true;
        ReversalPostingId = reversalPostingId;
        UpdatedAtUtc = nowUtc;
    }
}

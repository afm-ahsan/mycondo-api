using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.Features.Finance.BankReconciliations;

/// <summary>
/// One manually-entered line from a bank statement, belonging to a <see cref="BankReconciliation"/>
/// (Template 6). <see cref="Amount"/> is signed — positive increases the bank balance (a deposit/credit),
/// negative decreases it (a withdrawal/cheque/charge) — matching how a real statement reads, rather than
/// the ledger's own role-based debit/credit modeling. Manual entry only for this MVP; statement
/// import/parsing is an explicitly deferred follow-up (see the Template 6 execution notes).
/// </summary>
public sealed class BankStatementLine : AggregateRoot<BankStatementLineId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public BankReconciliationId BankReconciliationId { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public BankStatementLineStatus Status { get; private set; }
    public LedgerEntryId? MatchedLedgerEntryId { get; private set; }
    public LedgerPostingId? AdjustmentPostingId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private BankStatementLine()
    {
        Description = null!;
    }

    private BankStatementLine(
        BankStatementLineId id, Guid tenantId, BankReconciliationId bankReconciliationId, DateOnly transactionDate,
        string description, decimal amount) : base(id)
    {
        TenantId = tenantId;
        BankReconciliationId = bankReconciliationId;
        TransactionDate = transactionDate;
        Description = description;
        Amount = amount;
        Status = BankStatementLineStatus.Unmatched;
    }

    public static BankStatementLine Add(
        Guid tenantId, BankReconciliationId bankReconciliationId, DateOnly transactionDate, string description, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (amount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be zero.");
        }

        return new BankStatementLine(
            BankStatementLineId.New(), tenantId, bankReconciliationId, transactionDate, description.Trim(), amount);
    }

    private void EnsureUnmatched()
    {
        if (Status != BankStatementLineStatus.Unmatched)
        {
            throw new BankStatementLineNotUnmatchedException(Id, Status);
        }
    }

    public void MatchTo(LedgerEntryId ledgerEntryId)
    {
        EnsureUnmatched();
        MatchedLedgerEntryId = ledgerEntryId;
        Status = BankStatementLineStatus.Matched;
    }

    /// <summary>A statement line that requires no ledger action — a bank-side error, or a duplicate
    /// already accounted for by another line.</summary>
    public void Exclude(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureUnmatched();
        ResolutionNotes = reason.Trim();
        Status = BankStatementLineStatus.Excluded;
    }

    /// <summary>A statement line the ledger didn't yet know about (a bank charge, interest credit) —
    /// resolved by posting a brand-new ledger entry for it through the centralized posting service, then
    /// recording that posting's id here.</summary>
    public void MarkAdjusted(LedgerPostingId adjustmentPostingId)
    {
        EnsureUnmatched();
        AdjustmentPostingId = adjustmentPostingId;
        Status = BankStatementLineStatus.Adjusted;
    }
}

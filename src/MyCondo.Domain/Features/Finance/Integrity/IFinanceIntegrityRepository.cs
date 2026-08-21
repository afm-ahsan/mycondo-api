namespace MyCondo.Domain.Features.Finance.Integrity;

/// <summary>Raw, persisted-data checks for the Financial Integrity Dashboard (Template 6) — a control
/// surface over the existing ledger, not a second accounting engine. Every count here is expected to be
/// zero in a healthy tenant; each one is a live query against PostgreSQL rather than a re-assertion of a
/// domain invariant already enforced in memory, since the point is to catch a defect the in-memory
/// invariants failed to prevent (a bad migration, a manual DB edit, a bug), not to re-prove what
/// <c>LedgerPosting.Create</c> already guarantees on the happy path.</summary>
public interface IFinanceIntegrityRepository
{
    /// <summary>Ledger postings whose entries' debits don't sum to their credits — should never happen
    /// given <c>LedgerPosting.Create</c>'s constructor invariant; a non-zero count here means that
    /// invariant was bypassed somewhere (a raw SQL edit, a migration, a bug), not that it's optional.
    /// </summary>
    Task<long> CountUnbalancedPostingsAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Postings sharing the same (TenantId, ReferenceType, ReferenceId) with a non-null
    /// ReferenceId — should never happen given the partial unique index (ADR-027); a non-zero count here
    /// means idempotency was bypassed at the database level.</summary>
    Task<long> CountDuplicateLogicalPostingsAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Ledger entries whose stamped AccountingPeriod is Closed — should never happen given
    /// <c>FinancialPostingService.PostAsync</c>'s period-status check; a non-zero count here means a
    /// posting reached the ledger after its period was closed.</summary>
    Task<long> CountClosedPeriodViolationsAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Bank statement lines still Unmatched on an otherwise-InProgress reconciliation whose
    /// statement date is more than <paramref name="staleAfterDays"/> old — a genuine operational
    /// backlog, not a data-integrity defect, but exactly the kind of "needs attention" signal a
    /// governance dashboard exists to surface.</summary>
    Task<long> CountStaleUnreconciledBankItemsAsync(Guid tenantId, int staleAfterDays, CancellationToken cancellationToken);

    /// <summary>Fixed Deposit interest accruals with an outstanding (accrued but not yet received)
    /// balance more than <paramref name="staleAfterDays"/> old — flags interest that should probably
    /// have been received/recorded by now.</summary>
    Task<long> CountStaleUnreceivedInterestAccrualsAsync(Guid tenantId, int staleAfterDays, CancellationToken cancellationToken);
}

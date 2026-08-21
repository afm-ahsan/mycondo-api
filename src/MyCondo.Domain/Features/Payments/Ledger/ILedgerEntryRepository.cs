using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Ledger;

/// <summary>A ledger entry paired with its owning posting's ReferenceType/ReferenceId — lets callers
/// identify what originated a row (Invoice/Payment/PaymentReversal/OpeningBalance/InvoiceVoid/
/// UtilityBill) without parsing free-text Description. Same purpose-built-projection pattern as
/// Payroll's AttendanceRegisterEntry.</summary>
public sealed record LedgerEntryWithReference(LedgerEntry Entry, string? ReferenceType, Guid? ReferenceId);

public interface ILedgerEntryRepository
{
    void AddRange(IEnumerable<LedgerEntry> entries);

    Task<LedgerEntry?> GetByIdAsync(LedgerEntryId id, CancellationToken cancellationToken);

    /// <summary>Sum(debits) - sum(credits) for the flat's ResidentReceivable account — the balance is
    /// always derived from entries, never stored, per financial-engine.md's "closing balance is
    /// derived from ledger entries" rule.</summary>
    Task<decimal> GetReceivableBalanceForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    /// <summary>Same balance as <see cref="GetReceivableBalanceForFlatAsync"/> but strictly before
    /// <paramref name="asOfDate"/> — the Resident Financial Statement report's opening balance for a
    /// window starting on <paramref name="asOfDate"/>, mirroring
    /// <c>IFinanceReportRepository.GetAccountBalanceBeforeAsync</c>'s "before" semantics.</summary>
    Task<decimal> GetReceivableBalanceForFlatBeforeAsync(
        Guid tenantId, FlatId flatId, DateOnly asOfDate, CancellationToken cancellationToken);

    /// <summary>Raw (unsigned) debit/credit activity on the flat's ResidentReceivable account within
    /// [fromDate, toDate] (either bound may be null for an open end) — the Flat Financial Statement
    /// report's period debit/credit totals, distinct from <see cref="GetReceivableBalanceForFlatAsync"/>'s
    /// net balance.</summary>
    Task<(decimal TotalDebit, decimal TotalCredit)> GetReceivableActivityForFlatAsync(
        Guid tenantId, FlatId flatId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    /// <summary>Sum(credits) - sum(debits) for the flat's ResidentAdvance account — the resident's
    /// unallocated credit/overpayment balance (Billing↔Finance integration template §12), same
    /// ledger-derived-not-stored rule as <see cref="GetReceivableBalanceForFlatAsync"/>.</summary>
    Task<decimal> GetAdvanceBalanceForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    /// <summary>referenceType filters against the same finite vocabulary LedgerPosting.Create's
    /// callers actually use ("Invoice", "Payment", "PaymentReversal", "OpeningBalance", "InvoiceVoid",
    /// "UtilityBill") — this is the real "transaction type" signal for a flat-scoped ledger. AccountType
    /// is deliberately not a filter parameter here: LedgerPosting.Create only allows ResidentReceivable
    /// lines to carry a FlatId, so a flat-scoped query already only ever returns ResidentReceivable
    /// rows — an AccountType filter on this method would be a no-op.</summary>
    Task<PagedResult<LedgerEntryWithReference>> SearchForFlatAsync(
        Guid tenantId,
        FlatId flatId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? referenceType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Same population as <see cref="SearchForFlatAsync"/> but ordered oldest-first — the
    /// Resident Financial Statement report needs chronological order to build a running balance
    /// (<see cref="SearchForFlatAsync"/> is newest-first, matching the existing ledger-browse UX it was
    /// built for, and adding an ordering parameter there would change an existing, already-consumed
    /// signature). Same page-offset re-summation approach as <c>GetAccountLedgerQueryHandler</c> for the
    /// preceding-page running balance.</summary>
    Task<PagedResult<LedgerEntryWithReference>> SearchForFlatChronologicalAsync(
        Guid tenantId,
        FlatId flatId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

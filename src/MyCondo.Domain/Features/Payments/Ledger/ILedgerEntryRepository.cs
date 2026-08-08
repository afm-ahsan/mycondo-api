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

    /// <summary>Sum(debits) - sum(credits) for the flat's ResidentReceivable account — the balance is
    /// always derived from entries, never stored, per financial-engine.md's "closing balance is
    /// derived from ledger entries" rule.</summary>
    Task<decimal> GetReceivableBalanceForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

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
}

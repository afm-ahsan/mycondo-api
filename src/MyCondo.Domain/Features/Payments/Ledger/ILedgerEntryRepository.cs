using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Ledger;

public interface ILedgerEntryRepository
{
    void AddRange(IEnumerable<LedgerEntry> entries);

    /// <summary>Sum(debits) - sum(credits) for the flat's ResidentReceivable account — the balance is
    /// always derived from entries, never stored, per financial-engine.md's "closing balance is
    /// derived from ledger entries" rule.</summary>
    Task<decimal> GetReceivableBalanceForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    Task<PagedResult<LedgerEntry>> SearchForFlatAsync(
        Guid tenantId,
        FlatId flatId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

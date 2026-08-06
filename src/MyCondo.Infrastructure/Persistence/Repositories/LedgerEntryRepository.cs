using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class LedgerEntryRepository(MyCondoDbContext db) : ILedgerEntryRepository
{
    public void AddRange(IEnumerable<LedgerEntry> entries) => db.Set<LedgerEntry>().AddRange(entries);

    public async Task<decimal> GetReceivableBalanceForFlatAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId &&
                e.FlatId == flatId &&
                e.AccountType == LedgerAccountType.ResidentReceivable);

        decimal debits = await query
            .Where(e => e.Direction == LedgerDirection.Debit)
            .SumAsync(e => e.Amount, cancellationToken);

        decimal credits = await query
            .Where(e => e.Direction == LedgerDirection.Credit)
            .SumAsync(e => e.Amount, cancellationToken);

        return debits - credits;
    }

    public async Task<PagedResult<LedgerEntry>> SearchForFlatAsync(
        Guid tenantId, FlatId flatId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.FlatId == flatId);

        long total = await query.LongCountAsync(cancellationToken);

        List<LedgerEntry> items = await query
            .OrderByDescending(e => e.BusinessDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntry>(items, page, pageSize, total);
    }
}

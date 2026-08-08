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

    public async Task<PagedResult<LedgerEntryWithReference>> SearchForFlatAsync(
        Guid tenantId, FlatId flatId, DateOnly? fromDate, DateOnly? toDate, string? referenceType,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntryWithReference> query =
            from e in db.Set<LedgerEntry>().AsNoTracking()
            join p in db.Set<LedgerPosting>().AsNoTracking() on e.PostingId equals p.Id
            where e.TenantId == tenantId && e.FlatId == flatId
            select new LedgerEntryWithReference(e, p.ReferenceType, p.ReferenceId);

        if (fromDate is not null)
        {
            query = query.Where(x => x.Entry.BusinessDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(x => x.Entry.BusinessDate <= toDate);
        }

        if (referenceType is not null)
        {
            query = query.Where(x => x.ReferenceType == referenceType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<LedgerEntryWithReference> items = await query
            .OrderByDescending(x => x.Entry.BusinessDate)
            .ThenByDescending(x => x.Entry.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryWithReference>(items, page, pageSize, total);
    }
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class LedgerEntryRepository(MyCondoDbContext db) : ILedgerEntryRepository
{
    public void AddRange(IEnumerable<LedgerEntry> entries) => db.Set<LedgerEntry>().AddRange(entries);

    public Task<LedgerEntry?> GetByIdAsync(LedgerEntryId id, CancellationToken cancellationToken) =>
        db.Set<LedgerEntry>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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

    public async Task<decimal> GetReceivableBalanceForFlatBeforeAsync(
        Guid tenantId, FlatId flatId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId &&
                e.FlatId == flatId &&
                e.AccountType == LedgerAccountType.ResidentReceivable &&
                e.BusinessDate < asOfDate);

        decimal debits = await query
            .Where(e => e.Direction == LedgerDirection.Debit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        decimal credits = await query
            .Where(e => e.Direction == LedgerDirection.Credit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        return debits - credits;
    }

    public async Task<(decimal TotalDebit, decimal TotalCredit)> GetReceivableActivityForFlatAsync(
        Guid tenantId, FlatId flatId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId &&
                e.FlatId == flatId &&
                e.AccountType == LedgerAccountType.ResidentReceivable);

        if (fromDate is DateOnly from)
        {
            query = query.Where(e => e.BusinessDate >= from);
        }

        if (toDate is DateOnly to)
        {
            query = query.Where(e => e.BusinessDate <= to);
        }

        decimal totalDebit = await query
            .Where(e => e.Direction == LedgerDirection.Debit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        decimal totalCredit = await query
            .Where(e => e.Direction == LedgerDirection.Credit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        return (totalDebit, totalCredit);
    }

    public async Task<decimal> GetAdvanceBalanceForFlatAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId &&
                e.FlatId == flatId &&
                e.AccountType == LedgerAccountType.ResidentAdvance);

        decimal debits = await query
            .Where(e => e.Direction == LedgerDirection.Debit)
            .SumAsync(e => e.Amount, cancellationToken);

        decimal credits = await query
            .Where(e => e.Direction == LedgerDirection.Credit)
            .SumAsync(e => e.Amount, cancellationToken);

        return credits - debits;
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

    public async Task<PagedResult<LedgerEntryWithReference>> SearchForFlatChronologicalAsync(
        Guid tenantId, FlatId flatId, DateOnly? fromDate, DateOnly? toDate,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntryWithReference> query =
            from e in db.Set<LedgerEntry>().AsNoTracking()
            join p in db.Set<LedgerPosting>().AsNoTracking() on e.PostingId equals p.Id
            where e.TenantId == tenantId && e.FlatId == flatId && e.AccountType == LedgerAccountType.ResidentReceivable
            select new LedgerEntryWithReference(e, p.ReferenceType, p.ReferenceId);

        if (fromDate is not null)
        {
            query = query.Where(x => x.Entry.BusinessDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(x => x.Entry.BusinessDate <= toDate);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<LedgerEntryWithReference> items = await query
            .OrderBy(x => x.Entry.BusinessDate)
            .ThenBy(x => x.Entry.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryWithReference>(items, page, pageSize, total);
    }
}

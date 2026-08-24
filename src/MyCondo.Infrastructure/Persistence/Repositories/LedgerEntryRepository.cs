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
        IQueryable<LedgerEntry> entries = db.Set<LedgerEntry>().AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.FlatId == flatId);

        if (fromDate is not null)
        {
            entries = entries.Where(e => e.BusinessDate >= fromDate);
        }

        if (toDate is not null)
        {
            entries = entries.Where(e => e.BusinessDate <= toDate);
        }

        IQueryable<LedgerEntryWithReference> joined =
            from e in entries
            join p in db.Set<LedgerPosting>().AsNoTracking() on e.PostingId equals p.Id
            where referenceType == null || p.ReferenceType == referenceType
            orderby e.BusinessDate descending, e.CreatedAtUtc descending
            select new LedgerEntryWithReference(e, p.ReferenceType, p.ReferenceId);

        // Counted post-join (rather than on `entries` alone) so the referenceType filter — which lives
        // on LedgerPosting, not LedgerEntry — is reflected in Total. The ORDER BY is placed before this
        // record-constructing SELECT (not composed afterward via `query.OrderBy(x => x.Entry...)`) —
        // EF Core cannot translate an OrderBy/Where referencing a property path through a client
        // record freshly constructed by the immediately preceding SELECT (confirmed via
        // ExportContentVerificationTests: composing it the other way throws "could not be translated"
        // at query-compile time, a 500 on every call regardless of row count).
        long total = await joined.LongCountAsync(cancellationToken);

        List<LedgerEntryWithReference> items = await joined
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryWithReference>(items, page, pageSize, total);
    }

    public async Task<PagedResult<LedgerEntryWithReference>> SearchForFlatChronologicalAsync(
        Guid tenantId, FlatId flatId, DateOnly? fromDate, DateOnly? toDate,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> entries = db.Set<LedgerEntry>().AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.FlatId == flatId && e.AccountType == LedgerAccountType.ResidentReceivable);

        if (fromDate is not null)
        {
            entries = entries.Where(e => e.BusinessDate >= fromDate);
        }

        if (toDate is not null)
        {
            entries = entries.Where(e => e.BusinessDate <= toDate);
        }

        long total = await entries.LongCountAsync(cancellationToken);

        // Same EF-translatability constraint as SearchForFlatAsync above: ORDER BY must be composed
        // before the record-constructing SELECT, not after.
        List<LedgerEntryWithReference> items = await (
            from e in entries
            join p in db.Set<LedgerPosting>().AsNoTracking() on e.PostingId equals p.Id
            orderby e.BusinessDate, e.CreatedAtUtc
            select new LedgerEntryWithReference(e, p.ReferenceType, p.ReferenceId))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerEntryWithReference>(items, page, pageSize, total);
    }
}

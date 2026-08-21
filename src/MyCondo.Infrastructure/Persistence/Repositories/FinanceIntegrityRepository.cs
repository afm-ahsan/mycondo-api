using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Integrity;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FinanceIntegrityRepository(MyCondoDbContext db) : IFinanceIntegrityRepository
{
    public async Task<long> CountUnbalancedPostingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var balances =
            from e in db.Set<LedgerEntry>().AsNoTracking()
            where e.TenantId == tenantId
            group e by e.PostingId into g
            select new
            {
                Debits = g.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount),
                Credits = g.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount),
            };

        return await balances.LongCountAsync(b => b.Debits != b.Credits, cancellationToken);
    }

    public async Task<long> CountDuplicateLogicalPostingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        IQueryable<int> groups =
            from p in db.Set<LedgerPosting>().AsNoTracking()
            where p.TenantId == tenantId && p.ReferenceId != null
            group p by new { p.ReferenceType, p.ReferenceId } into g
            select g.Count();

        return await groups.LongCountAsync(count => count > 1, cancellationToken);
    }

    public async Task<long> CountClosedPeriodViolationsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntryId> query =
            from e in db.Set<LedgerEntry>().AsNoTracking()
            join p in db.Set<AccountingPeriod>().AsNoTracking() on e.AccountingPeriodId equals p.Id
            where e.TenantId == tenantId && p.Status == AccountingPeriodStatus.Closed && e.CreatedAtUtc > p.UpdatedAtUtc
            select e.Id;

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<long> CountStaleUnreconciledBankItemsAsync(Guid tenantId, int staleAfterDays, CancellationToken cancellationToken)
    {
        DateOnly threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-staleAfterDays));

        IQueryable<BankStatementLineId> query =
            from line in db.Set<BankStatementLine>().AsNoTracking()
            join reconciliation in db.Set<BankReconciliation>().AsNoTracking() on line.BankReconciliationId equals reconciliation.Id
            where line.TenantId == tenantId
                && line.Status == BankStatementLineStatus.Unmatched
                && reconciliation.Status == BankReconciliationStatus.InProgress
                && reconciliation.StatementDate < threshold
            select line.Id;

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<long> CountStaleUnreceivedInterestAccrualsAsync(Guid tenantId, int staleAfterDays, CancellationToken cancellationToken)
    {
        DateOnly threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-staleAfterDays));

        // Materialized client-side and joined in memory rather than as one composed EF query: at most
        // one row per Fixed Deposit (a small, tenant-scoped aggregate, not raw transaction volume), and
        // the LEFT JOIN + null-coalesce shape below does not translate to SQL through EF's query
        // provider (verified against real Postgres — see FinanceIntegrityRepositoryTests).
        Dictionary<Guid, decimal> accruedByFixedDeposit = await db.Set<FixedDepositInterestAccrual>()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsReversed && a.AccountingDate < threshold)
            .GroupBy(a => a.FixedDepositId)
            .Select(g => new { FixedDepositId = g.Key, Accrued = g.Sum(x => x.GrossAmount) })
            .ToDictionaryAsync(x => x.FixedDepositId.Value, x => x.Accrued, cancellationToken);

        Dictionary<Guid, decimal> receivedByFixedDeposit = await db.Set<FixedDepositInterestReceipt>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsReversed)
            .GroupBy(r => r.FixedDepositId)
            .Select(g => new { FixedDepositId = g.Key, Received = g.Sum(x => x.GrossAmount) })
            .ToDictionaryAsync(x => x.FixedDepositId.Value, x => x.Received, cancellationToken);

        return accruedByFixedDeposit.Count(kvp =>
            kvp.Value - receivedByFixedDeposit.GetValueOrDefault(kvp.Key) > 0m);
    }
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FixedDepositInterestAccrualRepository(MyCondoDbContext db) : IFixedDepositInterestAccrualRepository
{
    public Task<FixedDepositInterestAccrual?> GetByIdAsync(FixedDepositInterestAccrualId id, CancellationToken cancellationToken) =>
        db.Set<FixedDepositInterestAccrual>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<FixedDepositInterestAccrual>> GetForFixedDepositAsync(
        FixedDepositId fixedDepositId, CancellationToken cancellationToken) =>
        await db.Set<FixedDepositInterestAccrual>()
            .Where(x => x.FixedDepositId == fixedDepositId)
            .OrderBy(x => x.PeriodStart)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalAccruedAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken) =>
        await db.Set<FixedDepositInterestAccrual>()
            .Where(x => x.FixedDepositId == fixedDepositId && !x.IsReversed)
            .SumAsync(x => (decimal?)x.GrossAmount, cancellationToken) ?? 0m;

    public async Task<IReadOnlyList<FixedDepositAccrualTotal>> GetTotalsByFixedDepositAsync(
        Guid tenantId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        IQueryable<FixedDepositInterestAccrual> query = db.Set<FixedDepositInterestAccrual>()
            .Where(x => x.TenantId == tenantId && !x.IsReversed);

        if (fromDate is DateOnly from)
        {
            query = query.Where(x => x.AccountingDate >= from);
        }

        if (toDate is DateOnly to)
        {
            query = query.Where(x => x.AccountingDate <= to);
        }

        return await query
            .GroupBy(x => x.FixedDepositId)
            .Select(g => new FixedDepositAccrualTotal(g.Key, g.Count(), g.Sum(x => x.GrossAmount)))
            .ToListAsync(cancellationToken);
    }

    public void Add(FixedDepositInterestAccrual accrual) => db.Set<FixedDepositInterestAccrual>().Add(accrual);
}

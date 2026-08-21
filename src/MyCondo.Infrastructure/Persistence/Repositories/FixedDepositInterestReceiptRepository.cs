using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FixedDepositInterestReceiptRepository(MyCondoDbContext db) : IFixedDepositInterestReceiptRepository
{
    public Task<FixedDepositInterestReceipt?> GetByIdAsync(FixedDepositInterestReceiptId id, CancellationToken cancellationToken) =>
        db.Set<FixedDepositInterestReceipt>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<FixedDepositInterestReceipt>> GetForFixedDepositAsync(
        FixedDepositId fixedDepositId, CancellationToken cancellationToken) =>
        await db.Set<FixedDepositInterestReceipt>()
            .Where(x => x.FixedDepositId == fixedDepositId)
            .OrderBy(x => x.AccountingDate)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalReceivedGrossAsync(FixedDepositId fixedDepositId, CancellationToken cancellationToken) =>
        await db.Set<FixedDepositInterestReceipt>()
            .Where(x => x.FixedDepositId == fixedDepositId && !x.IsReversed)
            .SumAsync(x => (decimal?)x.GrossAmount, cancellationToken) ?? 0m;

    public async Task<IReadOnlyList<FixedDepositReceiptTotal>> GetTotalsByFixedDepositAsync(
        Guid tenantId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        IQueryable<FixedDepositInterestReceipt> query = db.Set<FixedDepositInterestReceipt>()
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
            .Select(g => new FixedDepositReceiptTotal(g.Key, g.Count(), g.Sum(x => x.GrossAmount), g.Sum(x => x.DeductionAmount)))
            .ToListAsync(cancellationToken);
    }

    public void Add(FixedDepositInterestReceipt receipt) => db.Set<FixedDepositInterestReceipt>().Add(receipt);
}

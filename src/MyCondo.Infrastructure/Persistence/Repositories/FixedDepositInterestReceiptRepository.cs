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

    public void Add(FixedDepositInterestReceipt receipt) => db.Set<FixedDepositInterestReceipt>().Add(receipt);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class BankReconciliationRepository(MyCondoDbContext db) : IBankReconciliationRepository
{
    public void Add(BankReconciliation reconciliation) => db.Set<BankReconciliation>().Add(reconciliation);

    public Task<BankReconciliation?> GetByIdAsync(BankReconciliationId id, CancellationToken cancellationToken) =>
        db.Set<BankReconciliation>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BankReconciliation>> GetForFinancialAccountAsync(
        FinancialAccountId financialAccountId, CancellationToken cancellationToken) =>
        await db.Set<BankReconciliation>()
            .Where(x => x.FinancialAccountId == financialAccountId)
            .OrderByDescending(x => x.StatementDate)
            .ToListAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.BankReconciliations;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class BankStatementLineRepository(MyCondoDbContext db) : IBankStatementLineRepository
{
    public void Add(BankStatementLine line) => db.Set<BankStatementLine>().Add(line);

    public Task<BankStatementLine?> GetByIdAsync(BankStatementLineId id, CancellationToken cancellationToken) =>
        db.Set<BankStatementLine>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BankStatementLine>> GetForReconciliationAsync(
        BankReconciliationId bankReconciliationId, CancellationToken cancellationToken) =>
        await db.Set<BankStatementLine>()
            .Where(x => x.BankReconciliationId == bankReconciliationId)
            .OrderBy(x => x.TransactionDate)
            .ToListAsync(cancellationToken);

    public Task<bool> HasUnresolvedLinesAsync(BankReconciliationId bankReconciliationId, CancellationToken cancellationToken) =>
        db.Set<BankStatementLine>().AnyAsync(
            x => x.BankReconciliationId == bankReconciliationId && x.Status == BankStatementLineStatus.Unmatched,
            cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ChartOfAccountRepository(MyCondoDbContext db) : IChartOfAccountRepository
{
    public void Add(ChartOfAccount account) => db.Set<ChartOfAccount>().Add(account);

    public Task<ChartOfAccount?> GetByIdAsync(ChartOfAccountId id, CancellationToken cancellationToken) =>
        db.Set<ChartOfAccount>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsForCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken) =>
        db.Set<ChartOfAccount>().AnyAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ChartOfAccount>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<ChartOfAccount>()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FinancialAccountRepository(MyCondoDbContext db) : IFinancialAccountRepository
{
    public Task<FinancialAccount?> GetByIdAsync(FinancialAccountId id, CancellationToken cancellationToken) =>
        db.Set<FinancialAccount>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<FinancialAccount>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<FinancialAccount>()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public void Add(FinancialAccount account) => db.Set<FinancialAccount>().Add(account);
}

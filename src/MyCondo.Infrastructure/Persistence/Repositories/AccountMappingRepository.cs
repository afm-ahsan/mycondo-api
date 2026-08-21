using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class AccountMappingRepository(MyCondoDbContext db) : IAccountMappingRepository
{
    public void Add(AccountMapping mapping) => db.Set<AccountMapping>().Add(mapping);

    public Task<AccountMapping?> GetByRoleAsync(Guid tenantId, string postingRole, CancellationToken cancellationToken) =>
        db.Set<AccountMapping>().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.PostingRole == postingRole, cancellationToken);

    public async Task<ChartOfAccountId?> ResolveAccountIdAsync(Guid tenantId, string postingRole, CancellationToken cancellationToken)
    {
        AccountMapping? mapping = await GetByRoleAsync(tenantId, postingRole, cancellationToken);
        return mapping?.ChartOfAccountId;
    }

    public async Task<IReadOnlyList<AccountMapping>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<AccountMapping>()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.PostingRole)
            .ToListAsync(cancellationToken);
}

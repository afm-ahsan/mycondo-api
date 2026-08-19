using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Domain.Features.Finance.AccountMappings;

public interface IAccountMappingRepository
{
    void Add(AccountMapping mapping);

    Task<AccountMapping?> GetByRoleAsync(Guid tenantId, string postingRole, CancellationToken cancellationToken);

    /// <summary>Resolves directly to the mapped account's id for the posting hot path — avoids a
    /// second round trip to load the <see cref="ChartOfAccount"/> itself when only the id is needed.</summary>
    Task<ChartOfAccountId?> ResolveAccountIdAsync(Guid tenantId, string postingRole, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountMapping>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}

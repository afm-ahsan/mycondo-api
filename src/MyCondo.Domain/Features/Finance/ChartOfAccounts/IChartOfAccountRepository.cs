namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

public interface IChartOfAccountRepository
{
    void Add(ChartOfAccount account);

    Task<ChartOfAccount?> GetByIdAsync(ChartOfAccountId id, CancellationToken cancellationToken);

    Task<bool> ExistsForCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChartOfAccount>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}

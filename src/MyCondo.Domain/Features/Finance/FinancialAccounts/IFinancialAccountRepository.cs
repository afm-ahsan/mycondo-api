namespace MyCondo.Domain.Features.Finance.FinancialAccounts;

public interface IFinancialAccountRepository
{
    Task<FinancialAccount?> GetByIdAsync(FinancialAccountId id, CancellationToken cancellationToken);

    Task<List<FinancialAccount>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(FinancialAccount account);
}

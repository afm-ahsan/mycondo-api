namespace MyCondo.Domain.Features.Finance.Funds;

public interface IFundRepository
{
    void Add(Fund fund);

    Task<bool> ExistsForCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<Fund>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}

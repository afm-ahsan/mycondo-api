namespace MyCondo.Domain.Features.Finance.Funds;

public interface IFundRepository
{
    void Add(Fund fund);

    Task<bool> ExistsForCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<Fund>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Added by Template 3 — Expense recording/updating needs to validate and resolve a
    /// caller-supplied <see cref="FundId"/> dimension.</summary>
    Task<Fund?> GetByIdAsync(FundId id, CancellationToken cancellationToken);
}

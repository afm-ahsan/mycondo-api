namespace MyCondo.Domain.Features.Finance.FinancialYears;

public interface IFinancialYearRepository
{
    void Add(FinancialYear financialYear);

    Task<FinancialYear?> GetByIdAsync(FinancialYearId id, CancellationToken cancellationToken);

    Task<bool> OverlapsAsync(Guid tenantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialYear>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}

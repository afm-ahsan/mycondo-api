using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FinancialYearRepository(MyCondoDbContext db) : IFinancialYearRepository
{
    public void Add(FinancialYear financialYear) => db.Set<FinancialYear>().Add(financialYear);

    public Task<FinancialYear?> GetByIdAsync(FinancialYearId id, CancellationToken cancellationToken) =>
        db.Set<FinancialYear>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> OverlapsAsync(Guid tenantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        db.Set<FinancialYear>().AnyAsync(
            x => x.TenantId == tenantId && x.StartDate <= endDate && x.EndDate >= startDate, cancellationToken);

    public async Task<IReadOnlyList<FinancialYear>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<FinancialYear>()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.StartDate)
            .ToListAsync(cancellationToken);
}

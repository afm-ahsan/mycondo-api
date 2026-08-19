using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class AccountingPeriodRepository(MyCondoDbContext db) : IAccountingPeriodRepository
{
    public void Add(AccountingPeriod period) => db.Set<AccountingPeriod>().Add(period);

    public Task<AccountingPeriod?> GetByIdAsync(AccountingPeriodId id, CancellationToken cancellationToken) =>
        db.Set<AccountingPeriod>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<AccountingPeriod?> FindCoveringAsync(Guid tenantId, DateOnly businessDate, CancellationToken cancellationToken) =>
        db.Set<AccountingPeriod>().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.StartDate <= businessDate && x.EndDate >= businessDate, cancellationToken);

    public Task<bool> OverlapsAsync(Guid tenantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
        db.Set<AccountingPeriod>().AnyAsync(
            x => x.TenantId == tenantId && x.StartDate <= endDate && x.EndDate >= startDate, cancellationToken);

    public async Task<IReadOnlyList<AccountingPeriod>> GetAllForFinancialYearAsync(FinancialYearId financialYearId, CancellationToken cancellationToken) =>
        await db.Set<AccountingPeriod>()
            .Where(x => x.FinancialYearId == financialYearId)
            .OrderBy(x => x.StartDate)
            .ToListAsync(cancellationToken);
}

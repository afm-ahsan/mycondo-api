using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FundRepository(MyCondoDbContext db) : IFundRepository
{
    public void Add(Fund fund) => db.Set<Fund>().Add(fund);

    public Task<bool> ExistsForCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken) =>
        db.Set<Fund>().AnyAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Fund>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<Fund>()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
}

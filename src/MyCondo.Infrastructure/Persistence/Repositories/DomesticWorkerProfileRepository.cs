using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class DomesticWorkerProfileRepository(MyCondoDbContext db) : IDomesticWorkerProfileRepository
{
    public Task<DomesticWorkerProfile?> GetByIdAsync(DomesticWorkerProfileId id, CancellationToken cancellationToken) =>
        db.Set<DomesticWorkerProfile>().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<PagedResult<DomesticWorkerProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<DomesticWorkerProfile> query = db.Set<DomesticWorkerProfile>()
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w =>
                EF.Functions.ILike(w.FullName, $"%{search}%") || EF.Functions.ILike(w.Phone, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<DomesticWorkerProfile> items = await query
            .OrderBy(w => w.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DomesticWorkerProfile>(items, page, pageSize, total);
    }

    public void Add(DomesticWorkerProfile profile) => db.Set<DomesticWorkerProfile>().Add(profile);
}

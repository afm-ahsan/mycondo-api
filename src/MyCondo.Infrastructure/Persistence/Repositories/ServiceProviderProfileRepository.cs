using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ServiceProviderProfileRepository(MyCondoDbContext db) : IServiceProviderProfileRepository
{
    public Task<ServiceProviderProfile?> GetByIdAsync(ServiceProviderProfileId id, CancellationToken cancellationToken) =>
        db.Set<ServiceProviderProfile>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<ServiceProviderProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ServiceProviderProfile> query = db.Set<ServiceProviderProfile>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.FullName, $"%{search}%") || EF.Functions.ILike(p.Phone, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<ServiceProviderProfile> items = await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceProviderProfile>(items, page, pageSize, total);
    }

    public void Add(ServiceProviderProfile profile) => db.Set<ServiceProviderProfile>().Add(profile);
}

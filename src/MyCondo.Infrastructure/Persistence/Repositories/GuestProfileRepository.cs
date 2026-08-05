using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GuestProfileRepository(MyCondoDbContext db) : IGuestProfileRepository
{
    public Task<GuestProfile?> GetByIdAsync(GuestProfileId id, CancellationToken cancellationToken) =>
        db.Set<GuestProfile>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GuestProfile?> GetByPhoneAsync(Guid tenantId, string phone, CancellationToken cancellationToken) =>
        db.Set<GuestProfile>().FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Phone == phone, cancellationToken);

    public async Task<PagedResult<GuestProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<GuestProfile> query = db.Set<GuestProfile>()
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
                EF.Functions.ILike(g.FullName, $"%{search}%") || EF.Functions.ILike(g.Phone, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GuestProfile> items = await query
            .OrderBy(g => g.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GuestProfile>(items, page, pageSize, total);
    }

    public void Add(GuestProfile guestProfile) => db.Set<GuestProfile>().Add(guestProfile);
}

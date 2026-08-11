using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FlatOwnershipRepository(MyCondoDbContext db) : IFlatOwnershipRepository
{
    public Task<FlatOwnership?> GetByIdAsync(FlatOwnershipId id, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<PagedResult<FlatOwnership>> SearchAsync(
        Guid tenantId,
        string? search,
        FlatOwnershipStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<FlatOwnership> query = db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId);

        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => db.Set<User>()
                .Any(u => u.Id == new UserId(o.UserId)
                    && (EF.Functions.ILike(u.FullName, $"%{search}%") || EF.Functions.ILike(u.Email, $"%{search}%"))));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<FlatOwnership> items = await query
            .OrderByDescending(o => o.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FlatOwnership>(items, page, pageSize, total);
    }

    public Task<List<FlatOwnership>> GetActiveForUserAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.UserId == userId && o.Status == FlatOwnershipStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<List<FlatOwnership>> GetAllForUserAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.UserId == userId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync(cancellationToken);

    public Task<List<FlatOwnership>> GetForFlatAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.FlatId == flatId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveForUserAndFlatAsync(
        Guid tenantId, Guid userId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>().AnyAsync(
            o => o.TenantId == tenantId && o.UserId == userId && o.FlatId == flatId
                && o.Status == FlatOwnershipStatus.Active,
            cancellationToken);

    public void Add(FlatOwnership ownership) => db.Set<FlatOwnership>().Add(ownership);
}

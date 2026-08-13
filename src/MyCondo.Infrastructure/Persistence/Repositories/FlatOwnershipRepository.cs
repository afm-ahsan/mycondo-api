using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

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
            query = query.Where(o => db.Set<Resident>()
                .Any(r => r.Id == new ResidentId(o.ResidentId)
                    && (EF.Functions.ILike(r.FullName, $"%{search}%")
                        || (r.Email != null && EF.Functions.ILike(r.Email, $"%{search}%"))
                        || (r.Phone != null && EF.Functions.ILike(r.Phone, $"%{search}%")))));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<FlatOwnership> items = await query
            .OrderByDescending(o => o.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FlatOwnership>(items, page, pageSize, total);
    }

    public Task<List<FlatOwnership>> GetActiveForResidentAsync(
        Guid tenantId, Guid residentId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.ResidentId == residentId && o.Status == FlatOwnershipStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<List<FlatOwnership>> GetAllForResidentAsync(
        Guid tenantId, Guid residentId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.ResidentId == residentId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync(cancellationToken);

    public Task<List<FlatOwnership>> GetForFlatAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.FlatId == flatId)
            .OrderByDescending(o => o.StartDate)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveForResidentAndFlatAsync(
        Guid tenantId, Guid residentId, FlatId flatId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>().AnyAsync(
            o => o.TenantId == tenantId && o.ResidentId == residentId && o.FlatId == flatId
                && o.Status == FlatOwnershipStatus.Active,
            cancellationToken);

    public void Add(FlatOwnership ownership) => db.Set<FlatOwnership>().Add(ownership);
}

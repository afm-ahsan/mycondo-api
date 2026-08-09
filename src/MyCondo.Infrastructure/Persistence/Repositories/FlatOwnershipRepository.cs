using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FlatOwnershipRepository(MyCondoDbContext db) : IFlatOwnershipRepository
{
    public Task<FlatOwnership?> GetByIdAsync(FlatOwnershipId id, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<List<FlatOwnership>> GetActiveForUserAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        db.Set<FlatOwnership>()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.UserId == userId && o.Status == FlatOwnershipStatus.Active)
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

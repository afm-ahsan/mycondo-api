using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(MyCondoDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken) =>
        db.Set<Vehicle>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehicle?> GetByRegistrationNumberAsync(
        Guid tenantId, string normalizedRegistrationNumber, CancellationToken cancellationToken) =>
        db.Set<Vehicle>().FirstOrDefaultAsync(
            v => v.TenantId == tenantId && v.RegistrationNumber == normalizedRegistrationNumber, cancellationToken);

    public async Task<PagedResult<Vehicle>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> query = db.Set<Vehicle>()
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => EF.Functions.ILike(v.RegistrationNumber, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Vehicle> items = await query
            .OrderBy(v => v.RegistrationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Vehicle>(items, page, pageSize, total);
    }

    public void Add(Vehicle vehicle) => db.Set<Vehicle>().Add(vehicle);
}

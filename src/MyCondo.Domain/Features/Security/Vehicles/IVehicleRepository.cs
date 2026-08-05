using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Security.Vehicles;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken);

    Task<Vehicle?> GetByRegistrationNumberAsync(
        Guid tenantId, string normalizedRegistrationNumber, CancellationToken cancellationToken);

    Task<PagedResult<Vehicle>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Vehicle vehicle);
}

using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehicleByRegistrationNumber;

public sealed class GetVehicleByRegistrationNumberQueryHandler(
    IVehicleRepository vehicles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetVehicleByRegistrationNumberQuery, VehicleDto?>
{
    public async ValueTask<VehicleDto?> Handle(GetVehicleByRegistrationNumberQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string normalized = Vehicle.NormalizeRegistrationNumber(query.RegistrationNumber);
        Vehicle? vehicle = await vehicles.GetByRegistrationNumberAsync(tenantId, normalized, cancellationToken);

        return vehicle is null
            ? null
            : new VehicleDto(
                vehicle.Id.Value, vehicle.RegistrationNumber, vehicle.VehicleType.ToString(), vehicle.Make,
                vehicle.Model, vehicle.Color, vehicle.OwnershipCategory.ToString(), vehicle.FlatId?.Value,
                vehicle.IsBlocked, vehicle.BlockReason);
    }
}

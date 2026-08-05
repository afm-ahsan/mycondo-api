using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.RegisterVehicle;

public sealed class RegisterVehicleCommandHandler(
    IVehicleRepository vehicles,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RegisterVehicleCommandHandler> logger
) : IRequestHandler<RegisterVehicleCommand, VehicleDto>
{
    public async ValueTask<VehicleDto> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId? flatId = null;
        if (command.FlatId is Guid rawFlatId)
        {
            flatId = new FlatId(rawFlatId);
            Flat flat = await flats.GetByIdAsync(flatId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Flat), rawFlatId);

            if (flat.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Flat), rawFlatId);
            }
        }

        string normalizedRegistrationNumber = Vehicle.NormalizeRegistrationNumber(command.RegistrationNumber);
        Vehicle? existing = await vehicles.GetByRegistrationNumberAsync(tenantId, normalizedRegistrationNumber, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A vehicle with registration '{normalizedRegistrationNumber}' already exists for this tenant.");
        }

        VehicleType vehicleType = Enum.Parse<VehicleType>(command.VehicleType);
        VehicleOwnershipCategory ownershipCategory = Enum.Parse<VehicleOwnershipCategory>(command.OwnershipCategory);

        Vehicle vehicle = Vehicle.Register(
            tenantId, command.RegistrationNumber, vehicleType, command.Make, command.Model, command.Color,
            ownershipCategory, flatId, clock.UtcNow);

        vehicles.Add(vehicle);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Vehicle {VehicleId} '{RegistrationNumber}' registered for tenant {TenantId}",
            vehicle.Id, vehicle.RegistrationNumber, tenantId);

        return new VehicleDto(
            vehicle.Id.Value, vehicle.RegistrationNumber, vehicle.VehicleType.ToString(), vehicle.Make,
            vehicle.Model, vehicle.Color, vehicle.OwnershipCategory.ToString(), vehicle.FlatId?.Value,
            vehicle.IsBlocked, vehicle.BlockReason);
    }
}

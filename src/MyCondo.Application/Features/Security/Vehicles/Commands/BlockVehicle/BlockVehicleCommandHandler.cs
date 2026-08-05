using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.BlockVehicle;

public sealed class BlockVehicleCommandHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<BlockVehicleCommandHandler> logger
) : IRequestHandler<BlockVehicleCommand>
{
    public async ValueTask<Unit> Handle(BlockVehicleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        VehicleId id = new(command.VehicleId);
        Vehicle vehicle = await vehicles.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), command.VehicleId);

        if (vehicle.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Vehicle), command.VehicleId);
        }

        vehicle.Block(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Vehicle {VehicleId} blocked for tenant {TenantId}: {Reason}", id, tenantId, command.Reason);

        return Unit.Value;
    }
}

using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.UnblockVehicle;

public sealed class UnblockVehicleCommandHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UnblockVehicleCommandHandler> logger
) : IRequestHandler<UnblockVehicleCommand>
{
    public async ValueTask<Unit> Handle(UnblockVehicleCommand command, CancellationToken cancellationToken)
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

        vehicle.Unblock();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Vehicle {VehicleId} unblocked for tenant {TenantId}", id, tenantId);

        return Unit.Value;
    }
}

using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockAdjustment;

public sealed class RecordStockAdjustmentCommandHandler(
    ICylinderStockMovementRepository movements,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordStockAdjustmentCommandHandler> logger
) : IRequestHandler<RecordStockAdjustmentCommand, CylinderStockMovementDto>
{
    public async ValueTask<CylinderStockMovementDto> Handle(RecordStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        CylinderStockMovement movement = CylinderStockMovement.Adjust(
            tenantId, command.CylinderType, command.SignedQuantity, command.Reason, command.OccurredAtUtc, currentUser.UserId,
            clock.UtcNow);

        movements.Add(movement);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Cylinder stock adjustment {CylinderStockMovementId} ({SignedQuantity}) recorded for {CylinderType}, tenant {TenantId}",
            movement.Id, command.SignedQuantity, command.CylinderType, tenantId);

        return movement.ToDto();
    }
}

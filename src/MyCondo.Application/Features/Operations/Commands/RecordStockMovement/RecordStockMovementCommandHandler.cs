using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockMovement;

public sealed class RecordStockMovementCommandHandler(
    ICylinderStockMovementRepository movements,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordStockMovementCommandHandler> logger
) : IRequestHandler<RecordStockMovementCommand, CylinderStockMovementDto>
{
    public async ValueTask<CylinderStockMovementDto> Handle(RecordStockMovementCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        CylinderPurchaseId? purchaseId = command.CylinderPurchaseId is Guid raw ? new CylinderPurchaseId(raw) : null;
        DateTimeOffset nowUtc = clock.UtcNow;

        CylinderStockMovement movement = command.MovementKind switch
        {
            "Receipt" => CylinderStockMovement.Receive(
                tenantId, command.CylinderType, command.Quantity, command.OccurredAtUtc, currentUser.UserId, purchaseId, nowUtc),
            "Issue" => CylinderStockMovement.Issue(
                tenantId, command.CylinderType, command.Quantity, command.OccurredAtUtc, currentUser.UserId, nowUtc),
            "EmptyReturn" => CylinderStockMovement.ReturnEmpty(
                tenantId, command.CylinderType, command.Quantity, command.OccurredAtUtc, currentUser.UserId, nowUtc),
            // Unreachable in practice — RecordStockMovementCommandValidator already restricts MovementKind
            // to these three values before the handler runs.
            _ => throw new InvalidOperationException($"Unrecognized MovementKind '{command.MovementKind}'.")
        };

        movements.Add(movement);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Cylinder stock movement {CylinderStockMovementId} ({MovementKind}) recorded for {CylinderType}, tenant {TenantId}",
            movement.Id, command.MovementKind, command.CylinderType, tenantId);

        return movement.ToDto();
    }
}

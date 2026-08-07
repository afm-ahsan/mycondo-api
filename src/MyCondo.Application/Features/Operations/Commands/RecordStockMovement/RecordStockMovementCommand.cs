using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockMovement;

/// <summary>MovementKind must be one of "Receipt", "Issue", "EmptyReturn" — Adjustment goes through
/// <c>RecordStockAdjustmentCommand</c> instead, since it needs a mandatory reason and the
/// <c>gascylinder.approve</c> permission (checked at the endpoint, a materially more sensitive action).</summary>
public sealed record RecordStockMovementCommand(
    string CylinderType,
    string MovementKind,
    int Quantity,
    DateTimeOffset OccurredAtUtc,
    Guid? CylinderPurchaseId
) : IRequest<CylinderStockMovementDto>;

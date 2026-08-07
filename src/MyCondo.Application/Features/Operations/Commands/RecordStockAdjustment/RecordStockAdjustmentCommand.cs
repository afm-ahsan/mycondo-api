using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockAdjustment;

public sealed record RecordStockAdjustmentCommand(
    string CylinderType,
    int SignedQuantity,
    string Reason,
    DateTimeOffset OccurredAtUtc
) : IRequest<CylinderStockMovementDto>;

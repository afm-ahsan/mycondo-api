namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record CylinderStockMovementDto(
    Guid CylinderStockMovementId,
    string CylinderType,
    string MovementType,
    int Quantity,
    DateTimeOffset OccurredAtUtc,
    string? Reason,
    Guid? RecordedBy,
    Guid? CylinderPurchaseId);

namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record MonthlyCylinderReconciliationDto(
    Guid MonthlyCylinderReconciliationId,
    string CylinderType,
    DateOnly PeriodMonth,
    int OpeningStock,
    int TotalReceived,
    int TotalIssued,
    int TotalEmptyReturned,
    int ClosingStock,
    int VarianceQuantity,
    string? Remarks,
    Guid? ReconciledBy,
    DateTimeOffset ReconciledAtUtc);

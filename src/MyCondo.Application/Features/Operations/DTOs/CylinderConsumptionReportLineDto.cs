namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record CylinderConsumptionReportLineDto(
    string CylinderType,
    int TotalReceived,
    int TotalIssued,
    int TotalEmptyReturned,
    int NetChange);

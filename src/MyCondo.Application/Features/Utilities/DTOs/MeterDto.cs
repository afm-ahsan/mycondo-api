namespace MyCondo.Application.Features.Utilities.DTOs;

public sealed record MeterDto(
    Guid MeterId,
    Guid BuildingId,
    string UtilityType,
    string MeterNumber,
    string Status,
    Guid? ReplacesMeterId);

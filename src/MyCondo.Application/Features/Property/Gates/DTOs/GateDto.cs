namespace MyCondo.Application.Features.Property.Gates.DTOs;

public sealed record GateDto(
    Guid GateId,
    Guid BuildingId,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    bool IsEntryAllowed,
    bool IsExitAllowed,
    int DisplayOrder);

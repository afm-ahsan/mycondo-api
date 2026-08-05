namespace MyCondo.Application.Features.Property.Gates.DTOs;

public sealed record GateDto(Guid GateId, Guid BuildingId, string Name);

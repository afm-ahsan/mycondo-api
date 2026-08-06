namespace MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;

public sealed record CreateBuildingResult(Guid BuildingId, string Name, string Code, string? Address);

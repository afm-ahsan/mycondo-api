using Mediator;
using MyCondo.Application.Features.Property.Buildings.DTOs;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;

public sealed record GetBuildingByIdQuery(Guid BuildingId) : IRequest<BuildingDto>;

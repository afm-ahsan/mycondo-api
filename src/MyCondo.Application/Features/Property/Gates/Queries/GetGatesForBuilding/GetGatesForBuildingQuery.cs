using Mediator;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Queries.GetGatesForBuilding;

public sealed record GetGatesForBuildingQuery(Guid BuildingId, bool ActiveOnly = false) : IRequest<List<GateDto>>;

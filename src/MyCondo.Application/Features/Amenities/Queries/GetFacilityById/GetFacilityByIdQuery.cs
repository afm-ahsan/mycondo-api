using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilityById;

public sealed record GetFacilityByIdQuery(Guid FacilityId) : IRequest<FacilityDto>;

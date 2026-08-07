using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetBlackoutDatesForFacility;

public sealed record GetBlackoutDatesForFacilityQuery(Guid FacilityId) : IRequest<IReadOnlyList<BlackoutDateDto>>;

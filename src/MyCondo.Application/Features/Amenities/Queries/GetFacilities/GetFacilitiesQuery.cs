using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilities;

public sealed record GetFacilitiesQuery(
    Guid? BuildingId,
    string? FacilityType,
    int Page,
    int PageSize
) : IRequest<PagedResult<FacilityDto>>;

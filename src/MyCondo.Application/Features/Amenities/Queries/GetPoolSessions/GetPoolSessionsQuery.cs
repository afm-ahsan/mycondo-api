using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;

public sealed record GetPoolSessionsQuery(
    Guid? FacilityId,
    Guid? FlatId,
    bool? OpenOnly,
    int Page,
    int PageSize
) : IRequest<PagedResult<PoolSessionDto>>;

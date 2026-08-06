using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetRatePlans;

public sealed record GetRatePlansQuery(
    Guid BuildingId,
    string? UtilityType,
    int Page,
    int PageSize
) : IRequest<PagedResult<RatePlanDto>>;

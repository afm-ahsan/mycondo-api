using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeters;

public sealed record GetMetersQuery(
    Guid BuildingId,
    string? UtilityType,
    int Page,
    int PageSize
) : IRequest<PagedResult<MeterDto>>;

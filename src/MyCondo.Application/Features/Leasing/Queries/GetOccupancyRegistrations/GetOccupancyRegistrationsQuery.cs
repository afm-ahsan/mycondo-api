using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

public sealed record GetOccupancyRegistrationsQuery(
    Guid? FlatId,
    string? Status,
    int Page,
    int PageSize
) : IRequest<PagedResult<OccupancyRegistrationDto>>;

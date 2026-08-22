using Mediator;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

public sealed record GetOccupancyRegistrationsQuery(
    Guid? FlatId,
    string? Status,
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<OccupancyRegistrationListItemDto>>;

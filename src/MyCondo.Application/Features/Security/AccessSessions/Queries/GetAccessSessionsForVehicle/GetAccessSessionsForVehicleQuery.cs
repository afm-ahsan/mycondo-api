using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForVehicle;

public sealed record GetAccessSessionsForVehicleQuery(
    Guid VehicleId,
    int Page,
    int PageSize
) : IRequest<PagedResult<AccessSessionDto>>;

using Mediator;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehiclesForTenant;

public sealed record GetVehiclesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<VehicleDto>>;

using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehiclesForTenant;

public sealed record GetVehiclesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<VehicleDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.vehicles";
}

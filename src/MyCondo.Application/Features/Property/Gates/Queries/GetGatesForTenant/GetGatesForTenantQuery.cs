using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Queries.GetGatesForTenant;

/// <summary>
/// Tenant-wide gate directory — unlike <see cref="GetGatesForBuilding.GetGatesForBuildingQuery"/>,
/// not scoped to one building.
/// </summary>
public sealed record GetGatesForTenantQuery(Guid? BuildingId, bool ActiveOnly = false)
    : IRequest<List<GateDto>>, IRequiresFeature
{
    public string FeatureKey => "security.gates";
}

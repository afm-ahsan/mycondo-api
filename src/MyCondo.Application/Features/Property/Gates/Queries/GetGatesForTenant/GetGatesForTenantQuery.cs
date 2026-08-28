using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Queries.GetGatesForTenant;

/// <summary>
/// Tenant-wide gate directory — unlike <see cref="GetGatesForBuilding.GetGatesForBuildingQuery"/>,
/// not scoped to one building. Also the non-core/feature-gated query pilot for the ADR-032 Task 10
/// lifecycle read/write classification, proving it composes with <see cref="IRequiresFeature"/>.
/// </summary>
public sealed record GetGatesForTenantQuery(Guid? BuildingId, bool ActiveOnly = false)
    : IRequest<List<GateDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.gates";
}

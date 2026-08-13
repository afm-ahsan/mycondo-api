using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForTenant;

/// <summary>
/// Tenant-wide flat directory backing the global Administration "Flats" page — unlike
/// <see cref="GetFlatsForBuilding.GetFlatsForBuildingQuery"/>, not scoped to one building.
/// </summary>
public sealed record GetFlatsForTenantQuery(
    string? Search,
    Guid? BuildingId,
    int Page,
    int PageSize
) : IRequest<PagedResult<FlatDto>>;

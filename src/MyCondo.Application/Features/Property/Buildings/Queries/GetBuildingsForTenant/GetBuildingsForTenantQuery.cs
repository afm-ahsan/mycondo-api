using Mediator;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;

public sealed record GetBuildingsForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<BuildingDto>>;

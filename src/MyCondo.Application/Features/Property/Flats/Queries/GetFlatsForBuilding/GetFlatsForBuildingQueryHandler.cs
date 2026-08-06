using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForBuilding;

public sealed class GetFlatsForBuildingQueryHandler(
    IFlatRepository flats,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatsForBuildingQuery, PagedResult<FlatDto>>
{
    public async ValueTask<PagedResult<FlatDto>> Handle(GetFlatsForBuildingQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);

        PagedResult<Flat> result = await flats.SearchAsync(
            tenantId, buildingId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<FlatDto> items = result.Items
            .Select(f => new FlatDto(f.Id.Value, f.BuildingId.Value, f.FlatNumber, f.FloorNumber, f.FlatType.ToString(), f.AreaSqFt))
            .ToList();

        return new PagedResult<FlatDto>(items, result.Page, result.PageSize, result.Total);
    }
}

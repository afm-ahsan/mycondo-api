using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilities;

public sealed class GetFacilitiesQueryHandler(
    IFacilityRepository facilities,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFacilitiesQuery, PagedResult<FacilityDto>>
{
    public async ValueTask<PagedResult<FacilityDto>> Handle(GetFacilitiesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        FacilityType? facilityType = query.FacilityType is null ? null : Enum.Parse<FacilityType>(query.FacilityType);

        PagedResult<Facility> result = await facilities.SearchAsync(
            tenantId, buildingId, facilityType, query.Page, query.PageSize, cancellationToken);

        List<FacilityDto> items = result.Items.Select(f => f.ToDto()).ToList();

        return new PagedResult<FacilityDto>(items, result.Page, result.PageSize, result.Total);
    }
}

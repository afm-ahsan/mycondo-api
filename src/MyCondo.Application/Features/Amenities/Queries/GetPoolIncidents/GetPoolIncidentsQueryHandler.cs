using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolIncidents;

public sealed class GetPoolIncidentsQueryHandler(
    IPoolIncidentRepository poolIncidents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPoolIncidentsQuery, PagedResult<PoolIncidentDto>>
{
    public async ValueTask<PagedResult<PoolIncidentDto>> Handle(GetPoolIncidentsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId? facilityId = query.FacilityId is Guid rawFacilityId ? new FacilityId(rawFacilityId) : null;

        PagedResult<PoolIncident> result = await poolIncidents.SearchAsync(
            tenantId, facilityId, query.Page, query.PageSize, cancellationToken);

        List<PoolIncidentDto> items = result.Items.Select(p => p.ToDto()).ToList();

        return new PagedResult<PoolIncidentDto>(items, result.Page, result.PageSize, result.Total);
    }
}

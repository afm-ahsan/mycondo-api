using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;

public sealed class GetPoolSessionsQueryHandler(
    IPoolSessionRepository poolSessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPoolSessionsQuery, PagedResult<PoolSessionDto>>
{
    public async ValueTask<PagedResult<PoolSessionDto>> Handle(GetPoolSessionsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId? facilityId = query.FacilityId is Guid rawFacilityId ? new FacilityId(rawFacilityId) : null;
        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;

        PagedResult<PoolSession> result = await poolSessions.SearchAsync(
            tenantId, facilityId, flatId, query.OpenOnly, query.Page, query.PageSize, cancellationToken);

        List<PoolSessionDto> items = result.Items.Select(p => p.ToDto()).ToList();

        return new PagedResult<PoolSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}

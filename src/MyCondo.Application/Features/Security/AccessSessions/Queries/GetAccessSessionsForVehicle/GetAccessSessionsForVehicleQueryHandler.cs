using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForVehicle;

public sealed class GetAccessSessionsForVehicleQueryHandler(
    IAccessSessionRepository accessSessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAccessSessionsForVehicleQuery, PagedResult<AccessSessionDto>>
{
    public async ValueTask<PagedResult<AccessSessionDto>> Handle(GetAccessSessionsForVehicleQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<AccessSession> result = await accessSessions.SearchForVehicleAsync(
            tenantId, new VehicleId(query.VehicleId), query.Page, query.PageSize, cancellationToken);

        List<AccessSessionDto> items = result.Items.Select(s => s.ToDto()).ToList();

        return new PagedResult<AccessSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}

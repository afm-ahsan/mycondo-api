using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Roles.DTOs;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Application.Features.Platform.Roles.Queries.GetPlatformRoles;

public sealed class GetPlatformRolesQueryHandler(
    IPlatformRoleRepository platformRoles,
    ICurrentPlatformUserProvider currentUser
) : IRequestHandler<GetPlatformRolesQuery, List<PlatformRoleSummaryDto>>
{
    public async ValueTask<List<PlatformRoleSummaryDto>> Handle(
        GetPlatformRolesQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<PlatformRole> allRoles = await platformRoles.GetAllAsync(cancellationToken);

        return allRoles
            .Select(r => new PlatformRoleSummaryDto(r.Id.Value, r.Name, r.Description))
            .ToList();
    }
}

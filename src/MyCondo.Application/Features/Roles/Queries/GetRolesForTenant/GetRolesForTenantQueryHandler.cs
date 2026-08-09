using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;

public sealed class GetRolesForTenantQueryHandler(
    IRoleRepository roles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetRolesForTenantQuery, List<RoleSummaryDto>>
{
    public async ValueTask<List<RoleSummaryDto>> Handle(GetRolesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<Role> tenantRoles = await roles.GetAllForTenantAsync(tenantId, cancellationToken);

        return tenantRoles
            .Select(r => new RoleSummaryDto(r.Id.Value, r.Name, r.Description, r.IsSystem, r.Code, r.RequiresBuildingScope))
            .ToList();
    }
}

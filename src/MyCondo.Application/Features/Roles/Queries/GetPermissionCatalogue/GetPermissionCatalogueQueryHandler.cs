using Mediator;
using MyCondo.Domain.Features.Identity.Permissions;

namespace MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

public sealed class GetPermissionCatalogueQueryHandler(
    IPermissionRepository permissions
) : IRequestHandler<GetPermissionCatalogueQuery, List<PermissionDto>>
{
    public async ValueTask<List<PermissionDto>> Handle(GetPermissionCatalogueQuery query, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);

        return catalogue
            .Select(p => new PermissionDto(p.Id.Value, p.Name, p.Description, p.Module, p.IsBuildingScopable))
            .ToList();
    }
}

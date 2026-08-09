using Mediator;
using MyCondo.Domain.Features.Identity.Permissions;

namespace MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

public sealed class GetPermissionCatalogueQueryHandler(
    IPermissionRepository permissions
) : IRequestHandler<GetPermissionCatalogueQuery, List<PermissionDto>>
{
    // See GrantPermissionToRoleCommandHandler's identical constant/comment — the tenant role-management
    // UI's permission picker must never even list platform.* permissions, let alone allow granting them.
    private const string PlatformPermissionModule = "platform";

    public async ValueTask<List<PermissionDto>> Handle(GetPermissionCatalogueQuery query, CancellationToken cancellationToken)
    {
        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);

        return catalogue
            .Where(p => !string.Equals(p.Module, PlatformPermissionModule, StringComparison.Ordinal))
            .Select(p => new PermissionDto(p.Id.Value, p.Name, p.Description, p.Module, p.IsBuildingScopable))
            .ToList();
    }
}

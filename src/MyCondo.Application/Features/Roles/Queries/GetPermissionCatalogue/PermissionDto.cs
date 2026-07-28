namespace MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

public sealed record PermissionDto(
    Guid Id,
    string Name,
    string Description,
    string Module,
    bool IsBuildingScopable
);

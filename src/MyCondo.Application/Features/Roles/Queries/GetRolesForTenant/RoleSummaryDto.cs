namespace MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;

public sealed record RoleSummaryDto(
    Guid RoleId,
    string Name,
    string Description,
    bool IsSystem
);

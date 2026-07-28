namespace MyCondo.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleResult(
    Guid RoleId,
    string Name,
    string Description
);

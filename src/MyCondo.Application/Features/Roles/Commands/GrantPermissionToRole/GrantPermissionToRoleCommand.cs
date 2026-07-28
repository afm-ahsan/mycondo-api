using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;

public sealed record GrantPermissionToRoleCommand(
    Guid RoleId,
    Guid PermissionId
) : IRequest;

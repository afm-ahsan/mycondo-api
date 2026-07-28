using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.RemovePermissionFromRole;

public sealed record RemovePermissionFromRoleCommand(
    Guid RoleId,
    Guid PermissionId
) : IRequest;

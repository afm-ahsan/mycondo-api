using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Commands.RemovePermissionFromRole;

public sealed record RemovePermissionFromRoleCommand(
    Guid RoleId,
    Guid PermissionId
) : IRequest, ILifecycleWriteOperation;

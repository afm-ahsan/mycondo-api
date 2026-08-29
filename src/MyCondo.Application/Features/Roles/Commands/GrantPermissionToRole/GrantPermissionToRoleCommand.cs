using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;

public sealed record GrantPermissionToRoleCommand(
    Guid RoleId,
    Guid PermissionId
) : IRequest, ILifecycleWriteOperation;

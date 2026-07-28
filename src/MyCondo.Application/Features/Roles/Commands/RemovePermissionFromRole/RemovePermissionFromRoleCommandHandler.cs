using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Commands.RemovePermissionFromRole;

public sealed class RemovePermissionFromRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<RemovePermissionFromRoleCommandHandler> logger
) : IRequestHandler<RemovePermissionFromRoleCommand>
{
    public async ValueTask<Unit> Handle(RemovePermissionFromRoleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        RoleId roleId = new(command.RoleId);
        Role role = await roles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), command.RoleId);

        if (role.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Role), command.RoleId);
        }

        if (role.IsSystem)
        {
            throw new ForbiddenException($"Cannot remove permissions from system role '{role.Name}'.");
        }

        PermissionId permissionId = new(command.PermissionId);
        Permission permission = await permissions.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Permission), command.PermissionId);

        RolePermission grant = await rolePermissions.GetAsync(roleId, permissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(RolePermission), command.PermissionId);

        rolePermissions.Remove(grant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Permission {PermissionName} removed from role {RoleId} for tenant {TenantId}",
            permission.Name, roleId, tenantId);

        return Unit.Value;
    }
}

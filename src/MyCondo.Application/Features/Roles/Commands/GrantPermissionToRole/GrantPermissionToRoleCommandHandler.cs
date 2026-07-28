using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;

public sealed class GrantPermissionToRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<GrantPermissionToRoleCommandHandler> logger
) : IRequestHandler<GrantPermissionToRoleCommand>
{
    public async ValueTask<Unit> Handle(GrantPermissionToRoleCommand command, CancellationToken cancellationToken)
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

        PermissionId permissionId = new(command.PermissionId);
        Permission permission = await permissions.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Permission), command.PermissionId);

        bool alreadyGranted = await rolePermissions.ExistsAsync(roleId, permissionId, cancellationToken);
        if (alreadyGranted)
        {
            throw new ConflictException($"Role '{role.Name}' already has permission '{permission.Name}'.");
        }

        RolePermission rolePermission = new(tenantId, roleId, permissionId, clock.UtcNow, currentUser.UserId);

        rolePermissions.Add(rolePermission);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Permission {PermissionName} granted to role {RoleId} for tenant {TenantId}",
            permission.Name, roleId, tenantId);

        return Unit.Value;
    }
}

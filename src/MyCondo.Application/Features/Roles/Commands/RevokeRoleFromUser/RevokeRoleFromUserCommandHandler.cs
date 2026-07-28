using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandHandler(
    IRoleRepository roles,
    IUserRepository users,
    IRoleAssignmentRepository roleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<RevokeRoleFromUserCommandHandler> logger
) : IRequestHandler<RevokeRoleFromUserCommand>
{
    public async ValueTask<Unit> Handle(RevokeRoleFromUserCommand command, CancellationToken cancellationToken)
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

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        RoleAssignment assignment = await roleAssignments.GetAsync(
                tenantId, userId, roleId, command.BuildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(RoleAssignment), command.UserId);

        if (role.IsSystem && command.BuildingId is null)
        {
            int holders = await roleAssignments.CountTenantWideHoldersAsync(tenantId, roleId, cancellationToken);
            if (holders <= 1)
            {
                throw new ConflictException(
                    $"Cannot revoke role '{role.Name}' — this tenant would be left with no one holding it.");
            }
        }

        roleAssignments.Remove(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Role {RoleId} revoked from user {UserId} for tenant {TenantId}", roleId, userId, tenantId);

        return Unit.Value;
    }
}

using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Commands.AssignPlatformRoleToUser;

public sealed class AssignPlatformRoleToUserCommandHandler(
    IPlatformRoleRepository platformRoles,
    IPlatformUserRepository platformUsers,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<AssignPlatformRoleToUserCommandHandler> logger
) : IRequestHandler<AssignPlatformRoleToUserCommand>
{
    public async ValueTask<Unit> Handle(AssignPlatformRoleToUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformRoleId roleId = new(command.RoleId);
        PlatformRole role = await platformRoles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformRole), command.RoleId);

        // mycondo-docs ADR-036 — granting the SuperAdmin role is itself a privileged mutation
        // regardless of who the target is (including self-promotion), so it requires
        // manageSuperAdmins on top of the base action permission.
        if (superAdminProtection.IsSuperAdmin(role) && !currentUser.HasPermission("platform.user.manageSuperAdmins"))
        {
            throw new ForbiddenException("Only a Platform Super Admin can grant the SuperAdmin role.");
        }

        PlatformUserId platformUserId = new(command.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), command.PlatformUserId);

        bool alreadyAssigned = await platformUserRoleAssignments.ExistsAsync(platformUserId, roleId, cancellationToken);
        if (alreadyAssigned)
        {
            throw new ConflictException($"Platform user already has role '{role.Name}'.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(platformUserId, roleId, nowUtc);

        platformUserRoleAssignments.Add(assignment);
        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Role.Assign", nameof(PlatformUser), platformUserId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { roleId = roleId.Value, roleName = role.Name })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Role {RoleId} assigned to platform user {PlatformUserId}", roleId, platformUserId);

        return Unit.Value;
    }
}

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

namespace MyCondo.Application.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;

public sealed class RevokePlatformRoleFromUserCommandHandler(
    IPlatformRoleRepository platformRoles,
    IPlatformUserRepository platformUsers,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<RevokePlatformRoleFromUserCommandHandler> logger
) : IRequestHandler<RevokePlatformRoleFromUserCommand>
{
    public async ValueTask<Unit> Handle(RevokePlatformRoleFromUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformRoleId roleId = new(command.RoleId);
        PlatformRole role = await platformRoles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformRole), command.RoleId);

        PlatformUserId platformUserId = new(command.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), command.PlatformUserId);

        List<PlatformUserRoleAssignment> assignments = await platformUserRoleAssignments.GetForUserAsync(
            platformUserId, cancellationToken);
        PlatformUserRoleAssignment assignment = assignments.FirstOrDefault(a => a.PlatformRoleId == roleId)
            ?? throw new NotFoundException(nameof(PlatformUserRoleAssignment), command.PlatformUserId);

        // mycondo-docs ADR-035 — revoking the SuperAdmin role (demotion) is subject to self-protection
        // and the manageSuperAdmins composition permission, on top of the last-holder protection.
        bool isSuperAdminRole = superAdminProtection.IsSuperAdmin(role);

        if (isSuperAdminRole)
        {
            superAdminProtection.EnsureCanMutateAdminTarget(
                platformUserId.Value, currentUser.PlatformUserId ?? Guid.Empty,
                currentUser.HasPermission("platform.user.manageSuperAdmins"));
        }

        await using IUnitOfWorkTransaction? transaction = isSuperAdminRole
            ? await unitOfWork.BeginTransactionAsync(cancellationToken)
            : null;

        if (isSuperAdminRole)
        {
            await superAdminProtection.EnsureNotLastActiveSuperAdminAsync(cancellationToken);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        platformUserRoleAssignments.Remove(assignment);
        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Role.Revoke", nameof(PlatformUser), platformUserId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { roleId = roleId.Value, roleName = role.Name })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Role {RoleId} revoked from platform user {PlatformUserId}", roleId, platformUserId);

        return Unit.Value;
    }
}

using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;

public sealed class CreatePlatformUserCommandHandler(
    IPlatformUserRepository platformUsers,
    IPlatformRoleRepository platformRoles,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPasswordHasher passwordHasher,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<CreatePlatformUserCommandHandler> logger
) : IRequestHandler<CreatePlatformUserCommand, CreatePlatformUserResult>
{
    public async ValueTask<CreatePlatformUserResult> Handle(
        CreatePlatformUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        PlatformUser? existing = await platformUsers.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' already exists.");
        }

        PlatformRole? initialRole = null;
        if (!string.IsNullOrWhiteSpace(command.InitialRoleName))
        {
            initialRole = await platformRoles.GetByNameAsync(command.InitialRoleName, cancellationToken)
                ?? throw new NotFoundException(nameof(PlatformRole), command.InitialRoleName);

            // mycondo-docs ADR-035 — granting the SuperAdmin role is itself a privileged mutation
            // regardless of who the target is, mirroring tenant AssignRoleToUserCommandHandler's
            // grant-time gate.
            if (superAdminProtection.IsSuperAdmin(initialRole)
                && !currentUser.HasPermission("platform.user.manageSuperAdmins"))
            {
                throw new ForbiddenException("Only a Platform Super Admin can grant the SuperAdmin role.");
            }
        }

        string passwordHash = passwordHasher.Hash(command.Password);
        DateTimeOffset nowUtc = clock.UtcNow;

        PlatformUser user = PlatformUser.Create(normalizedEmail, passwordHash, command.DisplayName, nowUtc);

        if (!command.IsActive)
        {
            user.Deactivate(nowUtc);
        }

        platformUsers.Add(user);

        if (initialRole is not null)
        {
            PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(
                user.Id, initialRole.Id, nowUtc);
            platformUserRoleAssignments.Add(assignment);
        }

        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Create", nameof(PlatformUser), user.Id.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Platform user {PlatformUserId} created", user.Id);

        return new CreatePlatformUserResult(user.Id.Value);
    }
}

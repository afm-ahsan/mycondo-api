using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Roles.Commands.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler(
    IRoleRepository roles,
    IUserRepository users,
    IBuildingRepository buildings,
    IRoleAssignmentRepository roleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AssignRoleToUserCommandHandler> logger
) : IRequestHandler<AssignRoleToUserCommand>
{
    public async ValueTask<Unit> Handle(AssignRoleToUserCommand command, CancellationToken cancellationToken)
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

        // Phase-2 scope enforcement (mycondo-docs ADR-020) — null means "no constraint," preserving
        // pre-Phase-2 behavior for every pre-existing role exactly as it was.
        if (role.RequiresBuildingScope == true && command.BuildingId is null)
        {
            throw new ConflictException($"Role '{role.Name}' requires a Building to be assigned.");
        }

        if (role.RequiresBuildingScope == false && command.BuildingId is not null)
        {
            throw new ConflictException($"Role '{role.Name}' is organization-wide and cannot be assigned to a specific Building.");
        }

        if (command.BuildingId is Guid buildingIdValue)
        {
            BuildingId buildingId = new(buildingIdValue);
            Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
                ?? throw new NotFoundException(nameof(Building), buildingIdValue);

            if (building.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Building), buildingIdValue);
            }
        }

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        bool alreadyAssigned = await roleAssignments.ExistsAsync(
            tenantId, userId, roleId, command.BuildingId, cancellationToken);
        if (alreadyAssigned)
        {
            throw new ConflictException($"User already has role '{role.Name}' for this scope.");
        }

        RoleAssignment assignment = RoleAssignment.Grant(
            tenantId, userId, roleId, command.BuildingId, clock.UtcNow);

        roleAssignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Role {RoleId} assigned to user {UserId} for tenant {TenantId}", roleId, userId, tenantId);

        return Unit.Value;
    }
}

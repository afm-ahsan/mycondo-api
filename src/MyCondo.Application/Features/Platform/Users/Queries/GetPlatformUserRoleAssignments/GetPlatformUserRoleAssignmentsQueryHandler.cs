using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.DTOs;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserRoleAssignments;

public sealed class GetPlatformUserRoleAssignmentsQueryHandler(
    IPlatformUserRepository platformUsers,
    IPlatformRoleRepository platformRoles,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments,
    ICurrentPlatformUserProvider currentUser
) : IRequestHandler<GetPlatformUserRoleAssignmentsQuery, List<PlatformUserRoleAssignmentDto>>
{
    public async ValueTask<List<PlatformUserRoleAssignmentDto>> Handle(
        GetPlatformUserRoleAssignmentsQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformUserId platformUserId = new(query.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), query.PlatformUserId);

        List<PlatformUserRoleAssignment> assignments = await platformUserRoleAssignments.GetForUserAsync(
            platformUserId, cancellationToken);
        List<PlatformRole> allRoles = await platformRoles.GetAllAsync(cancellationToken);
        Dictionary<PlatformRoleId, PlatformRole> rolesById = allRoles.ToDictionary(r => r.Id);

        return assignments
            .Where(a => rolesById.ContainsKey(a.PlatformRoleId))
            .Select(a =>
            {
                PlatformRole role = rolesById[a.PlatformRoleId];
                return new PlatformUserRoleAssignmentDto(role.Id.Value, role.Name, a.GrantedAtUtc);
            })
            .ToList();
    }
}

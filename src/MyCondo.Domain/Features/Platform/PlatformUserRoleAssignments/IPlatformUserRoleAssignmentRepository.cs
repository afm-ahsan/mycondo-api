using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;

public interface IPlatformUserRoleAssignmentRepository
{
    Task<List<PlatformUserRoleAssignment>> GetForUserAsync(
        PlatformUserId platformUserId, CancellationToken cancellationToken);

    void Add(PlatformUserRoleAssignment assignment);
}

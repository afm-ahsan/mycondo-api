using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Domain.Features.Identity.RoleAssignments;

public interface IRoleAssignmentRepository
{
    Task<bool> ExistsAsync(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        CancellationToken cancellationToken);

    void Add(RoleAssignment roleAssignment);
}

using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;

public interface IPlatformUserRoleAssignmentRepository
{
    Task<List<PlatformUserRoleAssignment>> GetForUserAsync(
        PlatformUserId platformUserId, CancellationToken cancellationToken);

    Task<List<PlatformUserRoleAssignment>> GetForRoleAsync(
        PlatformRoleId platformRoleId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        PlatformUserId platformUserId, PlatformRoleId platformRoleId, CancellationToken cancellationToken);

    /// <summary>
    /// Race-safe last-active-holder count for the given role — locks (<c>FOR UPDATE</c>) every
    /// currently-active holder's assignment row before counting, so a concurrent request against the
    /// same role's holder set must wait for this transaction to commit/roll back before it can read a
    /// consistent count. "Active" excludes assignments belonging to an already-<see cref="PlatformUserStatus.Disabled"/>
    /// <see cref="PlatformUsers.PlatformUser"/>. Must be called inside an explicit
    /// <see cref="MyCondo.Domain.Abstractions.IUnitOfWork"/> transaction (see mycondo-docs ADR-035 —
    /// last-active-admin invariant).
    /// </summary>
    Task<int> LockAndCountActiveSuperAdminHoldersAsync(
        PlatformRoleId platformRoleId, CancellationToken cancellationToken);

    void Add(PlatformUserRoleAssignment assignment);

    void Remove(PlatformUserRoleAssignment assignment);
}

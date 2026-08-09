using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;

/// <summary>
/// Links a <see cref="PlatformUser"/> to a <see cref="PlatformRole"/>. No scope column — unlike
/// <see cref="MyCondo.Domain.Features.Identity.RoleAssignments.RoleAssignment"/>'s optional
/// <c>BuildingId</c>, there is only one scope at this level (Platform), so nothing to discriminate.
/// </summary>
public sealed class PlatformUserRoleAssignment : Entity<PlatformUserRoleAssignmentId>, IAuditable
{
    public PlatformUserId PlatformUserId { get; private set; }
    public PlatformRoleId PlatformRoleId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private PlatformUserRoleAssignment() { }

    private PlatformUserRoleAssignment(
        PlatformUserRoleAssignmentId id,
        PlatformUserId platformUserId,
        PlatformRoleId platformRoleId,
        DateTimeOffset nowUtc) : base(id)
    {
        PlatformUserId = platformUserId;
        PlatformRoleId = platformRoleId;
        GrantedAtUtc = nowUtc;
        CreatedAtUtc = nowUtc;
    }

    public static PlatformUserRoleAssignment Grant(
        PlatformUserId platformUserId, PlatformRoleId platformRoleId, DateTimeOffset nowUtc) =>
        new(PlatformUserRoleAssignmentId.New(), platformUserId, platformRoleId, nowUtc);
}

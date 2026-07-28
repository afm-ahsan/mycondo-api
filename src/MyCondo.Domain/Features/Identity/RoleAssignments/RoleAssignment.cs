using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Domain.Features.Identity.RoleAssignments;

/// <summary>
/// Links a <see cref="User"/> to a <see cref="Role"/>, optionally scoped to a single building.
/// A user may hold the same role multiple times — once per building scope plus once globally.
/// </summary>
public sealed class RoleAssignment : Entity<RoleAssignmentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private RoleAssignment() { }

    private RoleAssignment(
        RoleAssignmentId id,
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        BuildingId = buildingId;
        GrantedAtUtc = nowUtc;
        CreatedAtUtc = nowUtc;
    }

    public static RoleAssignment Grant(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        DateTimeOffset nowUtc) =>
        new(RoleAssignmentId.New(), tenantId, userId, roleId, buildingId, nowUtc);
}

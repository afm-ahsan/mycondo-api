using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Domain.Features.Identity.RolePermissions;

/// <summary>
/// Links a <see cref="Role"/> to a <see cref="Permission"/>. Composite key (RoleId, PermissionId).
/// Granted/revoked via Application-layer commands; the Role aggregate stays focused on identity.
/// </summary>
public sealed class RolePermission
{
    public RoleId RoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public Guid? GrantedBy { get; private set; }

    private RolePermission() { }

    public RolePermission(RoleId roleId, PermissionId permissionId, DateTimeOffset nowUtc, Guid? grantedBy)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAtUtc = nowUtc;
        GrantedBy = grantedBy;
    }
}

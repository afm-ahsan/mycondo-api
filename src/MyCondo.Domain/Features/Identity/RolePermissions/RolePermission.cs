using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Domain.Features.Identity.RolePermissions;

/// <summary>
/// Links a <see cref="Role"/> to a <see cref="Permission"/>. Composite key (RoleId, PermissionId).
/// Granted/revoked via Application-layer commands; the Role aggregate stays focused on identity.
/// Denormalizes the owning Role's TenantId so this table can carry its own tenant_id column for RLS
/// (see mycondo-docs ADR-009) rather than needing a JOIN-based policy against roles.tenant_id.
/// </summary>
public sealed class RolePermission
{
    public Guid TenantId { get; private set; }
    public RoleId RoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public Guid? GrantedBy { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid tenantId, RoleId roleId, PermissionId permissionId, DateTimeOffset nowUtc, Guid? grantedBy)
    {
        TenantId = tenantId;
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAtUtc = nowUtc;
        GrantedBy = grantedBy;
    }
}

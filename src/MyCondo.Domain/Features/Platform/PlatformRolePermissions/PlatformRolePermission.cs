using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Domain.Features.Platform.PlatformRolePermissions;

/// <summary>
/// Links a <see cref="PlatformRole"/> to a <see cref="Permission"/>. Deliberately reuses the existing,
/// genuinely global <c>identity.permissions</c> catalog (confirmed to carry no <c>tenant_id</c> and no
/// RLS policy) rather than introducing a separate <c>PlatformPermission</c> entity — see mycondo-docs
/// ADR-019. Composite key (PlatformRoleId, PermissionId); no <c>TenantId</c> — unlike
/// <see cref="MyCondo.Domain.Features.Identity.RolePermissions.RolePermission"/>, there is no tenant
/// dimension to denormalize.
/// </summary>
public sealed class PlatformRolePermission
{
    public PlatformRoleId PlatformRoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public Guid? GrantedBy { get; private set; }

    private PlatformRolePermission() { }

    public PlatformRolePermission(
        PlatformRoleId platformRoleId, PermissionId permissionId, DateTimeOffset nowUtc, Guid? grantedBy)
    {
        PlatformRoleId = platformRoleId;
        PermissionId = permissionId;
        GrantedAtUtc = nowUtc;
        GrantedBy = grantedBy;
    }
}

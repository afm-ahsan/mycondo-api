using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Roles;

public sealed class Role : AggregateRoot<RoleId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Stable internal identifier for a system role (e.g. <c>"organization.admin"</c>,
    /// <c>"condominium.admin"</c>), distinct from the human-editable <see cref="Name"/>. Always null
    /// for <see cref="CreateCustom"/> roles. Introduced in Phase 2 (mycondo-docs ADR-020) so seeders
    /// can look up "does this tenant already have its OrganizationAdmin role" without depending on the
    /// display name never colliding with an unrelated custom role a tenant admin might create — Name
    /// remains what's shown in the UI and is otherwise the same immutable-for-system-roles value it
    /// always was.
    /// </summary>
    public string? Code { get; private set; }

    /// <summary>
    /// Phase-2 scope-enforcement flag (mycondo-docs ADR-020), set only for the system roles introduced
    /// there — every pre-existing role (legacy tenant <c>SuperAdmin</c>, and every
    /// <c>DefaultRoleCatalogueSeeder</c> custom role) keeps its default of <see langword="null"/>,
    /// meaning "no constraint," identical to today's behavior. <see langword="true"/> means
    /// <see cref="RoleAssignments.RoleAssignment.BuildingId"/> is required on every assignment of this
    /// role (condominium-scoped: CondoAdmin/Manager/Accountant/SecurityOfficer/FacilityManager);
    /// <see langword="false"/> means it must be null (organization/tenant-wide: OrganizationAdmin).
    /// Enforced in <c>AssignRoleToUserCommandHandler</c>, not here — see that handler's comment for why
    /// (this is cross-aggregate validation, matching how e.g. <c>CreateFacilityCommandHandler</c>
    /// validates Building/Tenant ownership at the application layer rather than inside a factory).
    /// </summary>
    public bool? RequiresBuildingScope { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Role()
    {
        Name = null!;
        Description = null!;
    }

    private Role(
        RoleId id,
        Guid tenantId,
        string name,
        string description,
        bool isSystem,
        string? code,
        bool? requiresBuildingScope,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsSystem = isSystem;
        Code = code;
        RequiresBuildingScope = requiresBuildingScope;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Role CreateCustom(
        Guid tenantId,
        string name,
        string description,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required for custom roles.", nameof(tenantId));
        }

        return new Role(
            RoleId.New(), tenantId, name.Trim(), description?.Trim() ?? string.Empty, false,
            code: null, requiresBuildingScope: null, nowUtc);
    }

    public static Role CreateSystem(
        RoleId id,
        Guid tenantId,
        string name,
        string description,
        DateTimeOffset nowUtc,
        string? code = null,
        bool? requiresBuildingScope = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role(
            id, tenantId, name.Trim(), description?.Trim() ?? string.Empty, true,
            code?.Trim(), requiresBuildingScope, nowUtc);
    }

    public void Rename(string newName)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be renamed.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        string trimmed = newName.Trim();
        if (string.Equals(Name, trimmed, StringComparison.Ordinal))
        {
            return;
        }

        Name = trimmed;
        Version++;
    }

    /// <summary>
    /// Soft-deletes the role (picked up by <c>RoleConfiguration</c>'s <c>DeletedAtUtc == null</c>
    /// query filter, same convention as every other <see cref="ISoftDeletable"/> entity). System
    /// roles — legacy tenant <c>SuperAdmin</c>, and since Phase 2, <c>OrganizationAdmin</c> and the
    /// condominium-scoped roles — can never be deactivated: doing so would strip every holder's access
    /// with no equivalent role to fall back to.
    /// </summary>
    public void Deactivate(DateTimeOffset nowUtc, Guid? deactivatedBy)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be deactivated.");
        }

        if (DeletedAtUtc is not null)
        {
            return;
        }

        DeletedAtUtc = nowUtc;
        DeletedBy = deactivatedBy;
    }
}

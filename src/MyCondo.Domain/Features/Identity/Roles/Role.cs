using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Roles;

public sealed class Role : AggregateRoot<RoleId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsSystem { get; private set; }
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
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsSystem = isSystem;
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

        return new Role(RoleId.New(), tenantId, name.Trim(), description?.Trim() ?? string.Empty, false, nowUtc);
    }

    public static Role CreateSystem(
        RoleId id,
        Guid tenantId,
        string name,
        string description,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role(id, tenantId, name.Trim(), description?.Trim() ?? string.Empty, true, nowUtc);
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
    /// roles — currently only <c>SuperAdmin</c> — can never be deactivated: doing so would strip every
    /// holder's access with no equivalent role to fall back to.
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

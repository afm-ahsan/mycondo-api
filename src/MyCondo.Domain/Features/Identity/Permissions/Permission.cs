using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Permissions;

/// <summary>
/// Permissions are reference data — same set across all tenants. Stored once globally,
/// granted to roles per tenant via <c>RolePermission</c>. Names follow
/// <c>&lt;module&gt;.&lt;resource&gt;.&lt;action&gt;</c> (e.g. <c>billing.invoice.create</c>).
/// </summary>
public sealed class Permission : Entity<PermissionId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Module { get; private set; }
    public bool IsBuildingScopable { get; private set; }

    private Permission()
    {
        Name = null!;
        Description = null!;
        Module = null!;
    }

    private Permission(PermissionId id, string name, string description, string module, bool isBuildingScopable)
        : base(id)
    {
        Name = name;
        Description = description;
        Module = module;
        IsBuildingScopable = isBuildingScopable;
    }

    public static Permission Create(
        PermissionId id,
        string name,
        string description,
        string module,
        bool isBuildingScopable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        return new Permission(id, name.Trim(), description?.Trim() ?? string.Empty, module.Trim(), isBuildingScopable);
    }
}

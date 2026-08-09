using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Platform.PlatformRoles;

/// <summary>
/// A role at Platform scope. Unlike <see cref="MyCondo.Domain.Features.Identity.Roles.Role"/>, this
/// has no <c>TenantId</c> and no scope field — the table's existence already means "Platform scope";
/// there is nothing else to discriminate at this level. See mycondo-docs ADR-019.
/// </summary>
public sealed class PlatformRole : AggregateRoot<PlatformRoleId>, IAuditable
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private PlatformRole()
    {
        Name = null!;
        Description = null!;
    }

    private PlatformRole(
        PlatformRoleId id,
        string name,
        string description,
        bool isSystem,
        DateTimeOffset nowUtc) : base(id)
    {
        Name = name;
        Description = description;
        IsSystem = isSystem;
        CreatedAtUtc = nowUtc;
    }

    public static PlatformRole CreateSystem(
        PlatformRoleId id,
        string name,
        string description,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PlatformRole(id, name.Trim(), description?.Trim() ?? string.Empty, true, nowUtc);
    }
}

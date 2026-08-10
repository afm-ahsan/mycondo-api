using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Tenancy;

/// <summary>
/// Records that a product module is enabled for a tenant. Platform-administered metadata about a
/// tenant (not tenant-owned data), so — like <c>tenancy.tenants</c> itself — this table carries no
/// RLS policy. Absence of a row for a given (tenant, module key) means the module is disabled.
/// </summary>
public sealed class TenantModule : Entity<TenantModuleId>
{
    public Guid TenantId { get; private set; }
    public string ModuleKey { get; private set; }
    public DateTimeOffset EnabledAtUtc { get; private set; }
    public Guid? EnabledBy { get; private set; }

    private TenantModule()
    {
        ModuleKey = null!;
    }

    private TenantModule(TenantModuleId id, Guid tenantId, string moduleKey, DateTimeOffset enabledAtUtc, Guid? enabledBy)
        : base(id)
    {
        TenantId = tenantId;
        ModuleKey = moduleKey;
        EnabledAtUtc = enabledAtUtc;
        EnabledBy = enabledBy;
    }

    public static TenantModule Enable(Guid tenantId, string moduleKey, DateTimeOffset nowUtc, Guid? enabledBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        if (!TenantModuleKeys.IsKnown(moduleKey))
        {
            throw new ArgumentException($"Unknown module key '{moduleKey}'.", nameof(moduleKey));
        }

        return new TenantModule(TenantModuleId.New(), tenantId, moduleKey, nowUtc, enabledBy);
    }
}

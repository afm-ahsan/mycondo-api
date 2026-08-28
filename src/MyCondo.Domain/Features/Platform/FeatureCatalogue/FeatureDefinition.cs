using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// A row in the commercial Feature Catalogue (ADR-033 §3) — a stable, machine-facing capability key
/// (e.g. <c>"finance.expenses"</c>) that a <c>SubscriptionPackageFeature</c> or <c>TenantFeatureOverride</c>
/// can target. Every one of the catalogue's 9 top-level groups is itself a row (<see cref="ParentFeatureId"/>
/// null); every leaf is a child of its group. Lives in the <c>platform</c> schema — not tenant data, so no
/// RLS, same reasoning as every other platform table (mycondo-docs ADR-019).
///
/// <see cref="IsCore"/> is the single mechanism for "must never be commercially disabled" (ADR-033 §6) —
/// no other code anywhere should special-case a feature key. This ADR's resolver (not built in this task)
/// short-circuits a core feature to enabled without ever consulting a package or override.
/// </summary>
public sealed class FeatureDefinition : Entity<FeatureDefinitionId>
{
    public string Key { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public FeatureDefinitionId? ParentFeatureId { get; private set; }
    public string Module { get; private set; }
    public FeatureCatalogueStatus Status { get; private set; }
    public bool IsCore { get; private set; }
    public int DisplayOrder { get; private set; }
    public FeatureEntitlementType EntitlementType { get; private set; }

    private FeatureDefinition()
    {
        Key = null!;
        Name = null!;
        Module = null!;
    }

    private FeatureDefinition(
        FeatureDefinitionId id,
        string key,
        string name,
        string? description,
        FeatureDefinitionId? parentFeatureId,
        string module,
        FeatureCatalogueStatus status,
        bool isCore,
        int displayOrder,
        FeatureEntitlementType entitlementType) : base(id)
    {
        Key = key;
        Name = name;
        Description = description;
        ParentFeatureId = parentFeatureId;
        Module = module;
        Status = status;
        IsCore = isCore;
        DisplayOrder = displayOrder;
        EntitlementType = entitlementType;
    }

    public static FeatureDefinition Create(
        string key,
        string name,
        string? description,
        FeatureDefinitionId? parentFeatureId,
        string module,
        FeatureCatalogueStatus status,
        bool isCore,
        int displayOrder,
        FeatureEntitlementType entitlementType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");
        }

        FeatureDefinitionId id = FeatureDefinitionId.New();

        // Structurally unreachable through this factory (the id is generated fresh above, so a caller
        // can never already know it to pass as its own parent) — kept as an explicit guard rather than
        // relying on that being true forever, per the task's "prevent obvious invalid catalogue states"
        // requirement.
        if (parentFeatureId == id)
        {
            throw new ArgumentException("A feature cannot be its own parent.", nameof(parentFeatureId));
        }

        return new FeatureDefinition(
            id, key.Trim(), name.Trim(), description?.Trim(), parentFeatureId, module.Trim(),
            status, isCore, displayOrder, entitlementType);
    }
}

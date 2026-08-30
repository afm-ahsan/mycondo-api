namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// Links a <see cref="FeatureDefinition"/> to an existing <c>identity.permissions</c> row by its natural
/// key (<see cref="PermissionCode"/> — no FK, mirrors <c>PermissionCatalogue</c>'s own "plain seeded
/// catalogue with no relational id" convention, ADR-033 §5). Composite key (FeatureId, PermissionCode).
///
/// This is administrative/UX metadata only — "this feature bundles these permissions," shown in a future
/// Feature Catalogue admin console. It is never read by backend entitlement enforcement: buying a feature
/// does not grant any permission, and a permission's presence here does not grant a feature (ADR-033 §4/§5).
/// </summary>
public sealed class FeaturePermission
{
    public FeatureDefinitionId FeatureId { get; private set; }
    public string PermissionCode { get; private set; }

    private FeaturePermission()
    {
        PermissionCode = null!;
    }

    public FeaturePermission(FeatureDefinitionId featureId, string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        FeatureId = featureId;
        PermissionCode = permissionCode.Trim();
    }
}

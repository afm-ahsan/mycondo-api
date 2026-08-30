using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Application.Common.Services;

public sealed class FeatureCatalogueSeeder(
    IFeatureDefinitionRepository featureDefinitions,
    IFeaturePermissionRepository featurePermissions,
    ILogger<FeatureCatalogueSeeder> logger
) : IFeatureCatalogueSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        ValidateCatalogueShape();

        List<FeatureDefinition> existingDefinitions = await featureDefinitions.GetAllAsync(cancellationToken);
        Dictionary<string, FeatureDefinitionId> idByKey = existingDefinitions
            .ToDictionary(f => f.Key, f => f.Id, StringComparer.Ordinal);

        int insertedDefinitions = 0;
        foreach ((string key, string name, string? description, string? parentKey, string module,
                     FeatureCatalogueStatus status, bool isCore, int displayOrder, FeatureEntitlementType entitlementType)
                 in FeatureCatalogue.Entries)
        {
            if (idByKey.ContainsKey(key))
            {
                continue;
            }

            FeatureDefinitionId? parentFeatureId = null;
            if (parentKey is not null)
            {
                if (!idByKey.TryGetValue(parentKey, out FeatureDefinitionId resolvedParentId))
                {
                    // Entries are declared parent-before-child, so a dangling reference here is a
                    // catalogue-authoring bug (invalid parent reference), not a runtime condition —
                    // ADR-033 §9's own "package-authoring-time validation" approach, applied here to the
                    // catalogue itself.
                    throw new InvalidOperationException(
                        $"FeatureCatalogue entry '{key}' references unknown parent key '{parentKey}'.");
                }

                parentFeatureId = resolvedParentId;
            }

            FeatureDefinition definition = FeatureDefinition.Create(
                key, name, description, parentFeatureId, module, status, isCore, displayOrder, entitlementType);

            featureDefinitions.Add(definition);
            idByKey[key] = definition.Id;
            insertedDefinitions++;
        }

        List<FeaturePermission> existingMappings = await featurePermissions.GetAllAsync(cancellationToken);
        HashSet<(FeatureDefinitionId FeatureId, string PermissionCode)> existingMappingKeys = existingMappings
            .Select(m => (m.FeatureId, m.PermissionCode))
            .ToHashSet();

        int insertedMappings = 0;
        foreach ((string featureKey, string permissionCode) in FeatureCatalogue.PermissionMappings)
        {
            if (!idByKey.TryGetValue(featureKey, out FeatureDefinitionId featureId))
            {
                throw new InvalidOperationException(
                    $"FeatureCatalogue.PermissionMappings references unknown feature key '{featureKey}'.");
            }

            if (existingMappingKeys.Contains((featureId, permissionCode)))
            {
                continue;
            }

            featurePermissions.Add(new FeaturePermission(featureId, permissionCode));
            existingMappingKeys.Add((featureId, permissionCode));
            insertedMappings++;
        }

        logger.LogInformation(
            "[DatabaseSeed] Feature Catalogue: {ExpectedDefinitions} feature(s) expected, {InsertedDefinitions} inserted; " +
            "{ExpectedMappings} mapping(s) expected, {InsertedMappings} inserted",
            FeatureCatalogue.Entries.Length, insertedDefinitions,
            FeatureCatalogue.PermissionMappings.Length, insertedMappings);
    }

    /// <summary>
    /// Fail-fast validation of the static catalogue data itself — duplicate keys and self-parent are
    /// "obvious invalid catalogue states" this task's own scope calls out to prevent, checked here since
    /// the static array has no compiler-enforced uniqueness. Dangling parent references are caught during
    /// the main reconciliation loop above instead, since detecting them here would just duplicate that walk.
    /// </summary>
    private static void ValidateCatalogueShape()
    {
        HashSet<string> seenKeys = new(StringComparer.Ordinal);
        foreach ((string key, string? parentKey) in FeatureCatalogue.Entries.Select(e => (e.Key, e.ParentKey)))
        {
            if (!seenKeys.Add(key))
            {
                throw new InvalidOperationException($"FeatureCatalogue has a duplicate key: '{key}'.");
            }

            if (string.Equals(key, parentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"FeatureCatalogue entry '{key}' cannot be its own parent.");
            }
        }
    }
}

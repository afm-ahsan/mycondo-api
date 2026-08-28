using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Authorization;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

public class FeatureCatalogueSeederTests
{
    private static (
        IFeatureDefinitionRepository FeatureDefinitions,
        IFeaturePermissionRepository FeaturePermissions,
        ILogger<FeatureCatalogueSeeder> Logger,
        List<FeatureDefinition> AddedDefinitions,
        List<FeaturePermission> AddedMappings)
        BuildSubstitute(IEnumerable<FeatureDefinition> existingDefinitions, IEnumerable<FeaturePermission> existingMappings)
    {
        IFeatureDefinitionRepository featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();
        IFeaturePermissionRepository featurePermissions = Substitute.For<IFeaturePermissionRepository>();
        ILogger<FeatureCatalogueSeeder> logger = Substitute.For<ILogger<FeatureCatalogueSeeder>>();

        featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(existingDefinitions.ToList());
        featurePermissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(existingMappings.ToList());

        List<FeatureDefinition> addedDefinitions = [];
        featureDefinitions.Add(Arg.Do<FeatureDefinition>(f => addedDefinitions.Add(f)));

        List<FeaturePermission> addedMappings = [];
        featurePermissions.Add(Arg.Do<FeaturePermission>(m => addedMappings.Add(m)));

        return (featureDefinitions, featurePermissions, logger, addedDefinitions, addedMappings);
    }

    [Fact]
    public async Task SeedAsync_On_Empty_Database_Inserts_Every_Catalogue_Entry_And_Mapping()
    {
        (IFeatureDefinitionRepository featureDefinitions, IFeaturePermissionRepository featurePermissions,
            ILogger<FeatureCatalogueSeeder> logger, List<FeatureDefinition> addedDefinitions, List<FeaturePermission> addedMappings) =
            BuildSubstitute([], []);

        FeatureCatalogueSeeder seeder = new(featureDefinitions, featurePermissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        addedDefinitions.Select(f => f.Key).Should().BeEquivalentTo(FeatureCatalogue.Entries.Select(e => e.Key));
        addedMappings.Should().HaveCount(FeatureCatalogue.PermissionMappings.Length);
    }

    [Fact]
    public async Task SeedAsync_Resolves_ParentFeatureId_To_The_Same_Run_Parent()
    {
        (IFeatureDefinitionRepository featureDefinitions, IFeaturePermissionRepository featurePermissions,
            ILogger<FeatureCatalogueSeeder> logger, List<FeatureDefinition> addedDefinitions, List<FeaturePermission> _) =
            BuildSubstitute([], []);

        FeatureCatalogueSeeder seeder = new(featureDefinitions, featurePermissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        FeatureDefinition financeRoot = addedDefinitions.Single(f => f.Key == "finance");
        FeatureDefinition financeBilling = addedDefinitions.Single(f => f.Key == "finance.billing");
        FeatureDefinition financeBanking = addedDefinitions.Single(f => f.Key == "finance.banking");
        FeatureDefinition financeFixedDeposits = addedDefinitions.Single(f => f.Key == "finance.fixed_deposits");

        financeBilling.ParentFeatureId.Should().Be(financeRoot.Id);
        financeBanking.ParentFeatureId.Should().Be(financeRoot.Id);
        financeFixedDeposits.ParentFeatureId.Should().Be(financeBanking.Id);
    }

    [Fact]
    public async Task SeedAsync_When_Catalogue_Already_Fully_Present_Inserts_Nothing()
    {
        List<FeatureDefinition> existingDefinitions = [];
        Dictionary<string, FeatureDefinitionId> idByKey = [];
        foreach ((string key, string name, string? description, string? parentKey, string module,
                     FeatureCatalogueStatus status, bool isCore, int displayOrder, FeatureEntitlementType entitlementType)
                 in FeatureCatalogue.Entries)
        {
            FeatureDefinitionId? parentId = parentKey is null ? null : idByKey[parentKey];
            FeatureDefinition definition = FeatureDefinition.Create(
                key, name, description, parentId, module, status, isCore, displayOrder, entitlementType);
            existingDefinitions.Add(definition);
            idByKey[key] = definition.Id;
        }

        List<FeaturePermission> existingMappings = FeatureCatalogue.PermissionMappings
            .Select(m => new FeaturePermission(idByKey[m.FeatureKey], m.PermissionCode))
            .ToList();

        (IFeatureDefinitionRepository featureDefinitions, IFeaturePermissionRepository featurePermissions,
            ILogger<FeatureCatalogueSeeder> logger, List<FeatureDefinition> addedDefinitions, List<FeaturePermission> addedMappings) =
            BuildSubstitute(existingDefinitions, existingMappings);

        FeatureCatalogueSeeder seeder = new(featureDefinitions, featurePermissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        addedDefinitions.Should().BeEmpty();
        addedMappings.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_Only_Inserts_Definitions_Missing_From_The_Database()
    {
        FeatureDefinition existingProperty = FeatureDefinition.Create(
            "property", "Property", null, null, "property", FeatureCatalogueStatus.Active, true, 100, FeatureEntitlementType.Boolean);

        (IFeatureDefinitionRepository featureDefinitions, IFeaturePermissionRepository featurePermissions,
            ILogger<FeatureCatalogueSeeder> logger, List<FeatureDefinition> addedDefinitions, List<FeaturePermission> _) =
            BuildSubstitute([existingProperty], []);

        FeatureCatalogueSeeder seeder = new(featureDefinitions, featurePermissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        addedDefinitions.Should().HaveCount(FeatureCatalogue.Entries.Length - 1);
        addedDefinitions.Select(f => f.Key).Should().NotContain("property");
    }

    [Fact]
    public async Task SeedAsync_Never_Removes_An_Unrecognized_Existing_Definition()
    {
        FeatureDefinition handInserted = FeatureDefinition.Create(
            "custom.made-up", "Custom", null, null, "custom", FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

        (IFeatureDefinitionRepository featureDefinitions, IFeaturePermissionRepository featurePermissions,
            ILogger<FeatureCatalogueSeeder> logger, List<FeatureDefinition> addedDefinitions, List<FeaturePermission> _) =
            BuildSubstitute([handInserted], []);

        FeatureCatalogueSeeder seeder = new(featureDefinitions, featurePermissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        // IFeatureDefinitionRepository has no Remove/Update member at all (additive-only by
        // construction) — this test's real assertion is that the seeder still inserts every catalogue
        // entry despite the unrecognized row's presence, i.e. it never short-circuits.
        addedDefinitions.Select(f => f.Key).Should().BeEquivalentTo(FeatureCatalogue.Entries.Select(e => e.Key));
    }
}

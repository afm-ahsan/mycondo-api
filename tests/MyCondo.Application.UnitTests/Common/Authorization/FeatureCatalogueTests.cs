using AwesomeAssertions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Application.UnitTests.Common.Authorization;

public class FeatureCatalogueTests
{
    [Fact]
    public void Entries_Has_No_Duplicate_Keys()
    {
        FeatureCatalogue.Entries.Select(e => e.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Entries_Has_No_Self_Parent()
    {
        FeatureCatalogue.Entries.Should().NotContain(e => e.Key == e.ParentKey);
    }

    [Fact]
    public void Entries_Every_ParentKey_Resolves_To_A_Real_Entry()
    {
        HashSet<string> keys = FeatureCatalogue.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        FeatureCatalogue.Entries
            .Where(e => e.ParentKey is not null)
            .Select(e => e.ParentKey!)
            .Should().OnlyContain(parentKey => keys.Contains(parentKey));
    }

    [Fact]
    public void Entries_Parents_Are_Declared_Before_Their_Children()
    {
        // The seeder resolves ParentFeatureId via an in-memory dictionary built while iterating Entries
        // in array order — a parent declared after its child would silently fail to resolve.
        HashSet<string> seenSoFar = new(StringComparer.Ordinal);

        foreach ((string key, string? parentKey) in FeatureCatalogue.Entries.Select(e => (e.Key, e.ParentKey)))
        {
            if (parentKey is not null)
            {
                seenSoFar.Should().Contain(parentKey, $"'{key}' must be declared after its parent '{parentKey}'");
            }

            seenSoFar.Add(key);
        }
    }

    [Fact]
    public void The_Nine_Task_A_Group_Roots_Exist_With_No_Parent()
    {
        string[] expectedGroups =
            ["property", "residents", "leasing", "security", "facilities", "utilities", "operations", "finance", "administration"];

        foreach (string group in expectedGroups)
        {
            FeatureCatalogue.Entries.Should().ContainSingle(e => e.Key == group && e.ParentKey == null);
        }
    }

    [Fact]
    public void Six_Reserved_Legacy_TenantModuleKeys_Are_Present_And_Not_Core()
    {
        string[] expectedReserved = ["vendors", "payroll", "complaints", "notifications", "documents", "maintenance"];

        foreach (string key in expectedReserved)
        {
            (string Key, string Name, string? Description, string? ParentKey, string Module,
                FeatureCatalogueStatus Status, bool IsCore, int DisplayOrder, FeatureEntitlementType EntitlementType)
                entry = FeatureCatalogue.Entries.Should().ContainSingle(e => e.Key == key).Subject;
            entry.Status.Should().Be(FeatureCatalogueStatus.Reserved);
            entry.IsCore.Should().BeFalse();
        }
    }

    [Fact]
    public void IsCore_Matches_ADR033_Section6_Exactly()
    {
        // ADR-033 §6's explicit IsCore set — the only mechanism for "must never be commercially
        // disabled." Deliberately does not include every group root (e.g. "residents"/"leasing"/
        // "finance" are not themselves core, even though several of their children are).
        string[] expectedCore =
        [
            "property",
            "residents.directory", "residents.flat_owners", "residents.self_service",
            "leasing.tenant_registration",
            "finance.billing", "finance.payments", "finance.expenses", "finance.resident_ledger",
            "finance.reports", "finance.governance",
            "administration", "administration.users", "administration.roles",
        ];

        FeatureCatalogue.Entries.Where(e => e.IsCore).Select(e => e.Key)
            .Should().BeEquivalentTo(expectedCore);
    }

    [Fact]
    public void Every_Feature_Seeded_Today_Is_Boolean_Entitlement_Type()
    {
        // ADR-033 §22 — Numeric (the ADR's "Integer") is reserved for a future limit.* feature; none
        // exists yet.
        FeatureCatalogue.Entries.Should().OnlyContain(e => e.EntitlementType == FeatureEntitlementType.Boolean);
    }

    [Fact]
    public void PermissionMappings_Every_FeatureKey_Exists_In_Entries()
    {
        HashSet<string> keys = FeatureCatalogue.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        FeatureCatalogue.PermissionMappings.Select(m => m.FeatureKey)
            .Should().OnlyContain(featureKey => keys.Contains(featureKey));
    }

    [Fact]
    public void PermissionMappings_Every_PermissionCode_Exists_In_The_Real_Permission_Catalogue()
    {
        HashSet<string> realPermissionCodes = PermissionCatalogue.Entries
            .Select(e => e.Name)
            .ToHashSet(StringComparer.Ordinal);

        FeatureCatalogue.PermissionMappings.Select(m => m.PermissionCode)
            .Should().OnlyContain(code => realPermissionCodes.Contains(code));
    }

    [Fact]
    public void PermissionMappings_Has_No_Duplicate_Pairs()
    {
        FeatureCatalogue.PermissionMappings.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void No_Reserved_Feature_Has_A_Permission_Mapping()
    {
        HashSet<string> reservedKeys = FeatureCatalogue.Entries
            .Where(e => e.Status == FeatureCatalogueStatus.Reserved)
            .Select(e => e.Key)
            .ToHashSet(StringComparer.Ordinal);

        FeatureCatalogue.PermissionMappings.Select(m => m.FeatureKey)
            .Should().NotContain(featureKey => reservedKeys.Contains(featureKey));
    }
}

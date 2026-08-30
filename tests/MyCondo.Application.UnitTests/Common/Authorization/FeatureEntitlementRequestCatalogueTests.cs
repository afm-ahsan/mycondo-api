using System.Runtime.CompilerServices;
using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Application.UnitTests.Common.Authorization;

/// <summary>
/// Enumerates every <see cref="IRequiresFeature"/>-implementing request in the Application assembly and
/// verifies its <c>FeatureKey</c> against the approved <see cref="FeatureCatalogue"/> (ADR-033 Task 06
/// §27/§41) — catches a typo'd/renamed/retired feature key at test time instead of it silently becoming a
/// permanent 403 for every caller of that request. <see cref="RuntimeHelpers.GetUninitializedObject"/>
/// reads the (constant, side-effect-free) <c>FeatureKey</c> getter without needing to satisfy each
/// request's real constructor arguments — safe here specifically because every <c>IRequiresFeature.FeatureKey</c>
/// implementation is a literal expression body, not derived from other instance state.
/// </summary>
public class FeatureEntitlementRequestCatalogueTests
{
    private static IReadOnlyList<Type> RequiresFeatureRequestTypes { get; } = typeof(DependencyInjection).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IRequiresFeature).IsAssignableFrom(t))
        .ToList();

    [Fact]
    public void At_Least_One_Request_Declares_IRequiresFeature()
    {
        // Guards against this test silently passing vacuously if every IRequiresFeature request were
        // ever removed/renamed without updating this assertion.
        RequiresFeatureRequestTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Every_IRequiresFeature_Request_Declares_A_Key_That_Exists_In_The_Feature_Catalogue()
    {
        HashSet<string> catalogueKeys = FeatureCatalogue.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        foreach (Type requestType in RequiresFeatureRequestTypes)
        {
            string featureKey = ReadFeatureKey(requestType);
            catalogueKeys.Should().Contain(featureKey,
                $"{requestType.FullName} declares FeatureKey '{featureKey}', which has no FeatureCatalogue entry");
        }
    }

    [Fact]
    public void Every_IRequiresFeature_Request_Declares_A_Key_That_Is_Active()
    {
        Dictionary<string, FeatureCatalogueStatus> statusByKey = FeatureCatalogue.Entries
            .ToDictionary(e => e.Key, e => e.Status, StringComparer.Ordinal);

        foreach (Type requestType in RequiresFeatureRequestTypes)
        {
            string featureKey = ReadFeatureKey(requestType);
            statusByKey.Should().ContainKey(featureKey);
            statusByKey[featureKey].Should().Be(FeatureCatalogueStatus.Active,
                $"{requestType.FullName} declares FeatureKey '{featureKey}', which is not Active");
        }
    }

    [Fact]
    public void Every_IRequiresFeature_Request_Declares_A_Key_That_Is_Not_Core()
    {
        // A core feature is never consulted for entitlement (ADR-033 §6) — marking a request behind one
        // with IRequiresFeature would be dead code that could never actually deny access.
        Dictionary<string, bool> isCoreByKey = FeatureCatalogue.Entries
            .ToDictionary(e => e.Key, e => e.IsCore, StringComparer.Ordinal);

        foreach (Type requestType in RequiresFeatureRequestTypes)
        {
            string featureKey = ReadFeatureKey(requestType);
            isCoreByKey.Should().ContainKey(featureKey);
            isCoreByKey[featureKey].Should().BeFalse(
                $"{requestType.FullName} declares FeatureKey '{featureKey}', which is a core feature");
        }
    }

    private static string ReadFeatureKey(Type requestType)
    {
        object instance = RuntimeHelpers.GetUninitializedObject(requestType);
        return ((IRequiresFeature)instance).FeatureKey;
    }
}

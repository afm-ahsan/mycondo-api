using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Maps the authoritative persisted <see cref="UtilityType"/> discriminator to its canonical
/// Feature Catalogue key (ADR-033 Task 09A §15-17/§20) — never inferred from meter display name, unit,
/// route, or permission string.</summary>
internal static class UtilityFeatureKeyMapper
{
    public static string ToFeatureKey(UtilityType utilityType) => utilityType switch
    {
        UtilityType.Electricity => "utilities.electricity",
        UtilityType.Gas => "utilities.gas",
        _ => throw new InvalidOperationException(
            $"UtilityType '{utilityType}' has no approved Feature Catalogue mapping."),
    };
}

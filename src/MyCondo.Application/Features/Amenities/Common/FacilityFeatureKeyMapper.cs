using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Maps the authoritative persisted <see cref="FacilityType"/> discriminator to its canonical
/// Feature Catalogue key (ADR-033 Task 09A §12/§20) — never inferred from facility name, request type, or
/// any other non-domain signal.</summary>
internal static class FacilityFeatureKeyMapper
{
    public static string ToFeatureKey(FacilityType facilityType) => facilityType switch
    {
        FacilityType.CommunityHall => "facilities.community_hall",
        FacilityType.SwimmingPool => "facilities.swimming_pool",
        _ => throw new InvalidOperationException(
            $"FacilityType '{facilityType}' has no approved Feature Catalogue mapping."),
    };
}

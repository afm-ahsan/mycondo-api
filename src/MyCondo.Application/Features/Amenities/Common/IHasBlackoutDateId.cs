namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Implemented by blackout-date requests that carry only <c>BlackoutDateId</c> — their commercial
/// feature is resolved via <see cref="BlackoutDateFacilityFeatureResolver{TRequest}"/> by following
/// BlackoutDate → Facility → FacilityType (ADR-033 Task 09A).</summary>
public interface IHasBlackoutDateId
{
    Guid BlackoutDateId { get; }
}

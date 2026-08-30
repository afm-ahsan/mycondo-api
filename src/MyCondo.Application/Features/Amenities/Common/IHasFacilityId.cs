namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Implemented by facility-scoped requests that carry a <c>FacilityId</c> directly — their
/// commercial feature (Community Hall vs. Swimming Pool) is resolved via
/// <see cref="FacilityFeatureResolver{TRequest}"/> (ADR-033 Task 09A).</summary>
public interface IHasFacilityId
{
    Guid FacilityId { get; }
}

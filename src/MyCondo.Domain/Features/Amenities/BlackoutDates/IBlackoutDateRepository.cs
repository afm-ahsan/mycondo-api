using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Domain.Features.Amenities.BlackoutDates;

public interface IBlackoutDateRepository
{
    Task<BlackoutDate?> GetByIdAsync(BlackoutDateId id, CancellationToken cancellationToken);

    /// <summary>Thin projection backing ADR-033 Task 09A resource-derived feature resolution — returns
    /// null (never the blackout date) when the id does not exist or is not owned by
    /// <paramref name="tenantId"/>, so a cross-tenant id yields the same not-found outcome as a genuinely
    /// missing one.</summary>
    Task<FacilityId?> GetFacilityIdAsync(Guid tenantId, BlackoutDateId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlackoutDate>> GetActiveForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlackoutDate>> ListForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken);

    void Add(BlackoutDate blackoutDate);
}

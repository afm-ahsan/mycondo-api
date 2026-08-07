using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Domain.Features.Amenities.BlackoutDates;

public interface IBlackoutDateRepository
{
    Task<BlackoutDate?> GetByIdAsync(BlackoutDateId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlackoutDate>> GetActiveForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlackoutDate>> ListForFacilityAsync(
        Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken);

    void Add(BlackoutDate blackoutDate);
}

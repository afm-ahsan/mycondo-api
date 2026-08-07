using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Amenities.Facilities;

public interface IFacilityRepository
{
    Task<Facility?> GetByIdAsync(FacilityId id, CancellationToken cancellationToken);

    Task<PagedResult<Facility>> SearchAsync(
        Guid tenantId, BuildingId? buildingId, FacilityType? facilityType, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Locks the facility row (<c>SELECT ... FOR UPDATE</c>) for the duration of the caller's
    /// transaction — used only by <c>CheckInPoolSessionCommandHandler</c> to serialize concurrent
    /// check-ins against the same facility, so the "count open sessions, compare to Capacity" check
    /// can't race the way a plain read would. Booking's equivalent concurrency guard is the DB's own
    /// <c>ex_bookings_no_overlap</c> EXCLUDE constraint; pool capacity has no such constraint (a count
    /// threshold isn't expressible as one), so this row lock is the guard instead. Must be called
    /// inside an open <see cref="MyCondo.Domain.Abstractions.IUnitOfWork.BeginTransactionAsync"/>
    /// transaction — the lock is held until commit/rollback.</summary>
    Task LockForCapacityCheckAsync(FacilityId id, CancellationToken cancellationToken);

    void Add(Facility facility);
}

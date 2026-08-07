using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Amenities.Facilities.Exceptions;

public sealed class FacilityInactiveException(FacilityId facilityId)
    : DomainException($"Facility {facilityId} is inactive and cannot be booked or accessed.")
{
    public FacilityId FacilityId { get; } = facilityId;
}

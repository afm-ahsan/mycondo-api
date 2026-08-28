using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Resolves the commercial FeatureKey for requests that carry only <c>BlackoutDateId</c>
/// (ADR-033 Task 09A §12) by following BlackoutDate → FacilityId → FacilityType with two thin tenant-scoped
/// projections (no full aggregate load). One dedicated implementation shared by the whole
/// "BlackoutDateId-only" request family via the generic constraint.</summary>
public sealed class BlackoutDateFacilityFeatureResolver<TRequest>(
    IBlackoutDateRepository blackoutDates,
    IFacilityRepository facilities
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasBlackoutDateId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        BlackoutDateId blackoutDateId = new(request.BlackoutDateId);
        FacilityId facilityId = await blackoutDates.GetFacilityIdAsync(tenantId, blackoutDateId, cancellationToken)
            ?? throw new NotFoundException(nameof(BlackoutDate), request.BlackoutDateId);

        FacilityType facilityType = await facilities.GetFacilityTypeAsync(tenantId, facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), facilityId.Value);

        return FacilityFeatureKeyMapper.ToFeatureKey(facilityType);
    }
}

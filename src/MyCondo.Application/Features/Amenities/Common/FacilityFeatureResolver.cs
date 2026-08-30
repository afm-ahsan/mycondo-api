using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Resolves the commercial FeatureKey for any request carrying a <c>FacilityId</c> directly
/// (ADR-033 Task 09A §12) — one dedicated implementation shared by the whole "FacilityId-only" request
/// family via the generic constraint, per request-family (not per request type).</summary>
public sealed class FacilityFeatureResolver<TRequest>(
    IFacilityRepository facilities
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasFacilityId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        FacilityId id = new(request.FacilityId);
        FacilityType facilityType = await facilities.GetFacilityTypeAsync(tenantId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), request.FacilityId);

        return FacilityFeatureKeyMapper.ToFeatureKey(facilityType);
    }
}

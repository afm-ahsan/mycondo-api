using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.GetBlackoutDatesForFacility;

public sealed class GetBlackoutDatesForFacilityQueryHandler(
    IFacilityRepository facilities,
    IBlackoutDateRepository blackoutDates,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBlackoutDatesForFacilityQuery, IReadOnlyList<BlackoutDateDto>>
{
    public async ValueTask<IReadOnlyList<BlackoutDateDto>> Handle(
        GetBlackoutDatesForFacilityQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId facilityId = new(query.FacilityId);
        Facility facility = await facilities.GetByIdAsync(facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), query.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), query.FacilityId);
        }

        IReadOnlyList<BlackoutDate> items = await blackoutDates.ListForFacilityAsync(tenantId, facilityId, cancellationToken);

        return items.Select(b => b.ToDto()).ToList();
    }
}

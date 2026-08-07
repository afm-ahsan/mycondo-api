using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilityById;

public sealed class GetFacilityByIdQueryHandler(
    IFacilityRepository facilities,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFacilityByIdQuery, FacilityDto>
{
    public async ValueTask<FacilityDto> Handle(GetFacilityByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId id = new(query.FacilityId);
        Facility facility = await facilities.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), query.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), query.FacilityId);
        }

        return facility.ToDto();
    }
}

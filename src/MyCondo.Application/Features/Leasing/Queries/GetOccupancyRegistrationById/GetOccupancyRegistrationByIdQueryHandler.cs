using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationById;

public sealed class GetOccupancyRegistrationByIdQueryHandler(
    IOccupancyRegistrationRepository registrations,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancyRegistrationByIdQuery, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        GetOccupancyRegistrationByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(query.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        }

        return registration.ToDto();
    }
}

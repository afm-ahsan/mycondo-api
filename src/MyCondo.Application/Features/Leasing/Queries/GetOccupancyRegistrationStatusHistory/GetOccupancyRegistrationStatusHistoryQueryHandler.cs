using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationStatusHistory;

public sealed class GetOccupancyRegistrationStatusHistoryQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IOccupancyRegistrationStatusHistoryRepository history,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancyRegistrationStatusHistoryQuery, IReadOnlyList<OccupancyRegistrationStatusHistoryDto>>
{
    public async ValueTask<IReadOnlyList<OccupancyRegistrationStatusHistoryDto>> Handle(
        GetOccupancyRegistrationStatusHistoryQuery query, CancellationToken cancellationToken)
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

        IReadOnlyList<OccupancyRegistrationStatusHistory> result = await history.GetForRegistrationAsync(id, cancellationToken);
        return result.Select(h => h.ToDto()).ToList();
    }
}

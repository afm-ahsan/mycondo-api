using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterAssignmentHistory;

public sealed class GetMeterAssignmentHistoryQueryHandler(
    IMeterRepository meters,
    IMeterAssignmentRepository assignments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMeterAssignmentHistoryQuery, IReadOnlyList<MeterAssignmentDto>>
{
    public async ValueTask<IReadOnlyList<MeterAssignmentDto>> Handle(
        GetMeterAssignmentHistoryQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId meterId = new(query.MeterId);
        Meter meter = await meters.GetByIdAsync(meterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), query.MeterId);
        if (meter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), query.MeterId);
        }

        IReadOnlyList<MeterAssignment> history = await assignments.GetHistoryForMeterAsync(tenantId, meterId, cancellationToken);

        return history.Select(a => a.ToDto()).ToList();
    }
}

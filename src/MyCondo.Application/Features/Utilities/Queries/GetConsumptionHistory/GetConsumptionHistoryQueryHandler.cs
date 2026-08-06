using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionHistory;

public sealed class GetConsumptionHistoryQueryHandler(
    IMeterRepository meters,
    IReadingRepository readings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetConsumptionHistoryQuery, IReadOnlyList<ReadingDto>>
{
    public async ValueTask<IReadOnlyList<ReadingDto>> Handle(
        GetConsumptionHistoryQuery query, CancellationToken cancellationToken)
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

        IReadOnlyList<Reading> history = await readings.GetConsumptionHistoryAsync(
            tenantId, meterId, query.FromDate, query.ToDate, cancellationToken);

        return history.Select(r => r.ToDto()).ToList();
    }
}

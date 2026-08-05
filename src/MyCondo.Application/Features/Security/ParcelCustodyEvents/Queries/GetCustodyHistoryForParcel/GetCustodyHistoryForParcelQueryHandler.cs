using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.DTOs;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;

public sealed class GetCustodyHistoryForParcelQueryHandler(
    IParcelCustodyEventRepository custodyEvents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCustodyHistoryForParcelQuery, List<ParcelCustodyEventDto>>
{
    public async ValueTask<List<ParcelCustodyEventDto>> Handle(GetCustodyHistoryForParcelQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<ParcelCustodyEvent> events = await custodyEvents.GetForParcelAsync(
            tenantId, new ParcelId(query.ParcelId), cancellationToken);

        return events
            .Select(e => new ParcelCustodyEventDto(
                e.Id.Value, e.ParcelId.Value, e.ToStatus.ToString(), e.OccurredAtUtc, e.PerformedBy, e.Notes))
            .ToList();
    }
}

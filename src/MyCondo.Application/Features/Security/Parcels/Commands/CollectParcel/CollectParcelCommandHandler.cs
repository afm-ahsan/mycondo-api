using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;

/// <summary>
/// Closes custody via handover. "Authorized collector" is enforced today via the endpoint's
/// parcel.handover permission plus the recorded collector name/acknowledgement — this slice does not
/// build OTP or e-signature verification (no such capability exists yet elsewhere in the platform).
/// </summary>
public sealed class CollectParcelCommandHandler(
    IParcelRepository parcels,
    IParcelCustodyEventRepository custodyEvents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CollectParcelCommandHandler> logger
) : IRequestHandler<CollectParcelCommand, ParcelDto>
{
    public async ValueTask<ParcelDto> Handle(CollectParcelCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ParcelId id = new(command.ParcelId);
        Parcel parcel = await parcels.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Parcel), command.ParcelId);
        if (parcel.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Parcel), command.ParcelId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        parcel.Collect(currentUser.UserId, command.CollectorName, command.Acknowledgement, nowUtc);
        custodyEvents.Add(ParcelCustodyEvent.Record(
            tenantId, id, ParcelStatus.Collected, currentUser.UserId, $"Collected by {command.CollectorName}", nowUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Parcel {ParcelId} collected by {CollectorName}, tenant {TenantId}",
            id, command.CollectorName, tenantId);

        return parcel.ToDto();
    }
}

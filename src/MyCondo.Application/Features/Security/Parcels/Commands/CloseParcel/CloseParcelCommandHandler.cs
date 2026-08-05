using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CloseParcel;

/// <summary>The endpoint gates on parcel.return (covers Returned/Rejected); LostOrEscalated
/// additionally requires parcel.escalate, checked here since one command covers all three outcomes.</summary>
public sealed class CloseParcelCommandHandler(
    IParcelRepository parcels,
    IParcelCustodyEventRepository custodyEvents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CloseParcelCommandHandler> logger
) : IRequestHandler<CloseParcelCommand, ParcelDto>
{
    public async ValueTask<ParcelDto> Handle(CloseParcelCommand command, CancellationToken cancellationToken)
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

        ParcelStatus outcome = Enum.Parse<ParcelStatus>(command.Outcome);

        if (outcome == ParcelStatus.LostOrEscalated && !currentUser.HasPermission("parcel.escalate"))
        {
            throw new ForbiddenException("Escalating a parcel as lost requires the parcel.escalate permission.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        parcel.Close(outcome, command.Reason);
        custodyEvents.Add(ParcelCustodyEvent.Record(tenantId, id, outcome, currentUser.UserId, command.Reason, nowUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Parcel {ParcelId} closed as {Outcome}, tenant {TenantId}", id, outcome, tenantId);

        return parcel.ToDto();
    }
}

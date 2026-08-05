using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.MarkParcelDamaged;

public sealed class MarkParcelDamagedCommandHandler(
    IParcelRepository parcels,
    IParcelCustodyEventRepository custodyEvents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<MarkParcelDamagedCommandHandler> logger
) : IRequestHandler<MarkParcelDamagedCommand, ParcelDto>
{
    public async ValueTask<ParcelDto> Handle(MarkParcelDamagedCommand command, CancellationToken cancellationToken)
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
        parcel.MarkDamaged(command.DamageNote);
        custodyEvents.Add(ParcelCustodyEvent.Record(
            tenantId, id, ParcelStatus.Damaged, currentUser.UserId, command.DamageNote, nowUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Parcel {ParcelId} marked damaged, tenant {TenantId}", id, tenantId);

        return parcel.ToDto();
    }
}

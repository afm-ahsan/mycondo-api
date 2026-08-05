using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.ReceiveParcel;

public sealed class ReceiveParcelCommandHandler(
    IParcelRepository parcels,
    IParcelCustodyEventRepository custodyEvents,
    IFlatRepository flats,
    IResidentRepository residents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReceiveParcelCommandHandler> logger
) : IRequestHandler<ReceiveParcelCommand, ParcelDto>
{
    public async ValueTask<ParcelDto> Handle(ReceiveParcelCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.RecipientFlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.RecipientFlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.RecipientFlatId);
        }

        ResidentId? residentId = null;
        if (command.RecipientResidentId is Guid rawResidentId)
        {
            residentId = new ResidentId(rawResidentId);
            Resident resident = await residents.GetByIdAsync(residentId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Resident), rawResidentId);
            if (resident.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Resident), rawResidentId);
            }
        }

        ParcelType parcelType = Enum.Parse<ParcelType>(command.ParcelType);
        DateTimeOffset nowUtc = clock.UtcNow;

        Parcel parcel = Parcel.Receive(
            tenantId, command.ParcelReference, command.CourierProvider, command.TrackingNumber,
            command.SenderName, flatId, residentId, parcelType, command.PackageCount, currentUser.UserId,
            command.StorageLocation, nowUtc);

        parcels.Add(parcel);
        custodyEvents.Add(ParcelCustodyEvent.Record(
            tenantId, parcel.Id, ParcelStatus.Received, currentUser.UserId, "Parcel received", nowUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Parcel {ParcelId} received for flat {FlatId}, tenant {TenantId}", parcel.Id, flatId, tenantId);

        return parcel.ToDto();
    }
}

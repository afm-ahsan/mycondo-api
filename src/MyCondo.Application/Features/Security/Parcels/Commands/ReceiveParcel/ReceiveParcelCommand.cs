using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.ReceiveParcel;

public sealed record ReceiveParcelCommand(
    string? ParcelReference,
    string? CourierProvider,
    string? TrackingNumber,
    string? SenderName,
    Guid RecipientFlatId,
    Guid? RecipientResidentId,
    string ParcelType,
    int PackageCount,
    string? StorageLocation
) : IRequest<ParcelDto>;

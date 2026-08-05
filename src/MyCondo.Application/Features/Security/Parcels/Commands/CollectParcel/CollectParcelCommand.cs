using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;

public sealed record CollectParcelCommand(
    Guid ParcelId,
    string CollectorName,
    string? Acknowledgement
) : IRequest<ParcelDto>;

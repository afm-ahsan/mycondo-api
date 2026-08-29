using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;

public sealed record CollectParcelCommand(
    Guid ParcelId,
    string CollectorName,
    string? Acknowledgement
) : IRequest<ParcelDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.parcels";
}

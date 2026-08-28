using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.MarkParcelDamaged;

public sealed record MarkParcelDamagedCommand(Guid ParcelId, string DamageNote) : IRequest<ParcelDto>, IRequiresFeature
{
    public string FeatureKey => "security.parcels";
}

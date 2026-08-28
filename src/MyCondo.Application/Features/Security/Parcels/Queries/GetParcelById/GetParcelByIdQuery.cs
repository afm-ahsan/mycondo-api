using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelById;

public sealed record GetParcelByIdQuery(Guid ParcelId) : IRequest<ParcelDto>, IRequiresFeature
{
    public string FeatureKey => "security.parcels";
}

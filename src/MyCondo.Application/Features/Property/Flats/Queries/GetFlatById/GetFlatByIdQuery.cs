using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatById;

public sealed record GetFlatByIdQuery(Guid FlatId) : IRequest<FlatDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "property.flats";
}

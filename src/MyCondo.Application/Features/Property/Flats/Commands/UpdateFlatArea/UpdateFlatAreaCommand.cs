using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;

public sealed record UpdateFlatAreaCommand(Guid FlatId, decimal? AreaSqFt) : IRequest<FlatDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "property.flats";
}

using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;

public sealed record DeactivateFlatCommand(Guid FlatId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "property.flats";
}

using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;

public sealed record SetFlatPrimaryPhotoCommand(
    Guid FlatId, Guid? AttachmentId
) : IRequest<FlatDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "property.flats";
}

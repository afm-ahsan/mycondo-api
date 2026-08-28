using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Buildings.DTOs;

namespace MyCondo.Application.Features.Property.Buildings.Commands.SetBuildingPrimaryPhoto;

public sealed record SetBuildingPrimaryPhotoCommand(
    Guid BuildingId, Guid? AttachmentId
) : IRequest<BuildingDto>, IRequiresFeature
{
    public string FeatureKey => "property.buildings";
}

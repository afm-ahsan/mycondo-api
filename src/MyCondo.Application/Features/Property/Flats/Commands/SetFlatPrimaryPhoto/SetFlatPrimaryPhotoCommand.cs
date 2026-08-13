using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;

public sealed record SetFlatPrimaryPhotoCommand(
    Guid FlatId, Guid? AttachmentId
) : IRequest<FlatDto>;

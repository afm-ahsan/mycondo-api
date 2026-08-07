using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.SetOccupancyRegistrationPrimaryPhoto;

public sealed record SetOccupancyRegistrationPrimaryPhotoCommand(
    Guid OccupancyRegistrationId, Guid? AttachmentId
) : IRequest<OccupancyRegistrationDto>;

using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByOwner;

public sealed record RequestOccupancyRegistrationCorrectionsByOwnerCommand(
    Guid OccupancyRegistrationId, string Reason
) : IRequest<OccupancyRegistrationDto>;

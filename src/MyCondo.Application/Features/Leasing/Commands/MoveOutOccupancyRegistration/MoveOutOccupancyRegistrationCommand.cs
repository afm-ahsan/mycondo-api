using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;

public sealed record MoveOutOccupancyRegistrationCommand(
    Guid OccupancyRegistrationId, string? Reason
) : IRequest<OccupancyRegistrationDto>;

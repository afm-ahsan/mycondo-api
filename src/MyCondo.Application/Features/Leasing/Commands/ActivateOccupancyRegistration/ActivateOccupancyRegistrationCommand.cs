using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.ActivateOccupancyRegistration;

public sealed record ActivateOccupancyRegistrationCommand(Guid OccupancyRegistrationId) : IRequest<OccupancyRegistrationDto>;

using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.SubmitOccupancyRegistration;

public sealed record SubmitOccupancyRegistrationCommand(Guid OccupancyRegistrationId) : IRequest<OccupancyRegistrationDto>;

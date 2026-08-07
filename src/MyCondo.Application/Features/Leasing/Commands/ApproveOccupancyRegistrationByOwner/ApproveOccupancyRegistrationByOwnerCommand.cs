using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.ApproveOccupancyRegistrationByOwner;

public sealed record ApproveOccupancyRegistrationByOwnerCommand(Guid OccupancyRegistrationId) : IRequest<OccupancyRegistrationDto>;

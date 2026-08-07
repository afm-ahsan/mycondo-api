using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.DeactivateBlackoutDate;

public sealed record DeactivateBlackoutDateCommand(Guid BlackoutDateId) : IRequest<BlackoutDateDto>;

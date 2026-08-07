using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;

public sealed record CheckOutPoolSessionCommand(Guid PoolSessionId) : IRequest<PoolSessionDto>;

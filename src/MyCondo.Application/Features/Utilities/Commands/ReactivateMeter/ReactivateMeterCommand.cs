using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;

public sealed record ReactivateMeterCommand(Guid MeterId) : IRequest<MeterDto>;

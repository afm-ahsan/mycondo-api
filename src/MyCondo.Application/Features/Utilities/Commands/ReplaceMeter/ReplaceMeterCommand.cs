using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.ReplaceMeter;

public sealed record ReplaceMeterCommand(Guid MeterId, string NewMeterNumber) : IRequest<ReplaceMeterResultDto>;

using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;

public sealed record MarkMeterFaultyCommand(Guid MeterId, string Reason) : IRequest<MeterDto>;

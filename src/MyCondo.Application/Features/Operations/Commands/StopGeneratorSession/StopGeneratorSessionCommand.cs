using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.StopGeneratorSession;

public sealed record StopGeneratorSessionCommand(
    Guid GeneratorSessionId,
    decimal ClosingFuelLevel,
    string? OutageReason,
    decimal? HourMeterReading
) : IRequest<GeneratorSessionDto>;

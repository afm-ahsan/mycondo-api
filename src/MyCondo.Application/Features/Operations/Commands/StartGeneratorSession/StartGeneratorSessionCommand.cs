using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;

public sealed record StartGeneratorSessionCommand(
    Guid GeneratorId,
    decimal OpeningFuelLevel
) : IRequest<GeneratorSessionDto>;

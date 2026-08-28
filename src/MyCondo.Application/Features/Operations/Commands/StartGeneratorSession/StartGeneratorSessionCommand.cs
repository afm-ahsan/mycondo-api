using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;

public sealed record StartGeneratorSessionCommand(
    Guid GeneratorId,
    decimal OpeningFuelLevel
) : IRequest<GeneratorSessionDto>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}

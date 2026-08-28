using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateGenerator;

public sealed record DeactivateGeneratorCommand(Guid GeneratorId) : IRequest<GeneratorDto>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}

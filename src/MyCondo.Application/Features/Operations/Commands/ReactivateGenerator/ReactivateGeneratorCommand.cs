using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ReactivateGenerator;

public sealed record ReactivateGeneratorCommand(Guid GeneratorId) : IRequest<GeneratorDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "operations.generator";
}

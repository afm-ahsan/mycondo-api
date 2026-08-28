using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorById;

public sealed record GetGeneratorByIdQuery(Guid GeneratorId) : IRequest<GeneratorDto>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}

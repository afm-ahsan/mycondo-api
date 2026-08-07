using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorById;

public sealed record GetGeneratorByIdQuery(Guid GeneratorId) : IRequest<GeneratorDto>;

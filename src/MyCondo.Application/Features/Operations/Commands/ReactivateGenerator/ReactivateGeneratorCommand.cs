using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ReactivateGenerator;

public sealed record ReactivateGeneratorCommand(Guid GeneratorId) : IRequest<GeneratorDto>;

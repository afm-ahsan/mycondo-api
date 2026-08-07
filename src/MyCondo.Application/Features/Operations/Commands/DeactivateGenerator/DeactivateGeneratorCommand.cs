using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateGenerator;

public sealed record DeactivateGeneratorCommand(Guid GeneratorId) : IRequest<GeneratorDto>;

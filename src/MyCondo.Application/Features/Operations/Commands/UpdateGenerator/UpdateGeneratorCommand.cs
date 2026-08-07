using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.UpdateGenerator;

public sealed record UpdateGeneratorCommand(
    Guid GeneratorId,
    string Name,
    string? Model,
    decimal? CapacityKva,
    string? Location
) : IRequest<GeneratorDto>;

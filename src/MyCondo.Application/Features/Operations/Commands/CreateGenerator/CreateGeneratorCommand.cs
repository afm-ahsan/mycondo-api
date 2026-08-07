using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.CreateGenerator;

public sealed record CreateGeneratorCommand(
    Guid BuildingId,
    string Name,
    string? Model,
    decimal? CapacityKva,
    string? Location
) : IRequest<GeneratorDto>;

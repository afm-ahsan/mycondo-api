using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetCurrentStock;

public sealed record GetCurrentStockQuery(string? CylinderType) : IRequest<IReadOnlyList<CylinderStockDto>>;

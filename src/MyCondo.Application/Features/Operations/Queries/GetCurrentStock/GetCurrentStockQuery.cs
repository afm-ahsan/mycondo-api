using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetCurrentStock;

public sealed record GetCurrentStockQuery(string? CylinderType) : IRequest<IReadOnlyList<CylinderStockDto>>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}

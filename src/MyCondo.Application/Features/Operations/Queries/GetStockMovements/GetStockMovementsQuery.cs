using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(
    string? CylinderType,
    int Page,
    int PageSize
) : IRequest<PagedResult<CylinderStockMovementDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "operations.gas_cylinders";
}

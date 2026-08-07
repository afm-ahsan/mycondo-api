using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Application.Features.Operations.Queries.GetCurrentStock;

public sealed class GetCurrentStockQueryHandler(
    ICylinderStockMovementRepository movements,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCurrentStockQuery, IReadOnlyList<CylinderStockDto>>
{
    public async ValueTask<IReadOnlyList<CylinderStockDto>> Handle(GetCurrentStockQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<string> cylinderTypes = string.IsNullOrWhiteSpace(query.CylinderType)
            ? await movements.ListDistinctCylinderTypesAsync(tenantId, cancellationToken)
            : [query.CylinderType];

        List<CylinderStockDto> results = [];
        foreach (string cylinderType in cylinderTypes)
        {
            int stock = await movements.GetCurrentStockAsync(tenantId, cylinderType, cancellationToken);
            results.Add(new CylinderStockDto(cylinderType, stock));
        }

        return results;
    }
}

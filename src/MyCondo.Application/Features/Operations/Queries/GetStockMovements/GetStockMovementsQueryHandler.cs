using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Application.Features.Operations.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler(
    ICylinderStockMovementRepository movements,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetStockMovementsQuery, PagedResult<CylinderStockMovementDto>>
{
    public async ValueTask<PagedResult<CylinderStockMovementDto>> Handle(GetStockMovementsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<CylinderStockMovement> result = await movements.SearchAsync(
            tenantId, query.CylinderType, query.Page, query.PageSize, cancellationToken);

        List<CylinderStockMovementDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<CylinderStockMovementDto>(items, result.Page, result.PageSize, result.Total);
    }
}

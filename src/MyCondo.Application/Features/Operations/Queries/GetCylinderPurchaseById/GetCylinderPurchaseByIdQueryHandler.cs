using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Features.Operations.CylinderPurchases;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchaseById;

public sealed class GetCylinderPurchaseByIdQueryHandler(
    ICylinderPurchaseRepository purchases,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCylinderPurchaseByIdQuery, CylinderPurchaseDto>
{
    public async ValueTask<CylinderPurchaseDto> Handle(GetCylinderPurchaseByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        CylinderPurchase purchase = await purchases.GetByIdAsync(new CylinderPurchaseId(query.CylinderPurchaseId), cancellationToken)
            ?? throw new NotFoundException(nameof(CylinderPurchase), query.CylinderPurchaseId);
        if (purchase.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(CylinderPurchase), query.CylinderPurchaseId);
        }

        return purchase.ToDto();
    }
}

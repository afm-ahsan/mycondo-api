using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderPurchases;

public sealed class GetCylinderPurchasesQueryHandler(
    ICylinderPurchaseRepository purchases,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCylinderPurchasesQuery, PagedResult<CylinderPurchaseDto>>
{
    public async ValueTask<PagedResult<CylinderPurchaseDto>> Handle(GetCylinderPurchasesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GasCylinderSupplierId? supplierId = query.SupplierId is Guid raw ? new GasCylinderSupplierId(raw) : null;
        CylinderPurchaseApprovalStatus? approvalStatus = query.ApprovalStatus is null
            ? null
            : Enum.Parse<CylinderPurchaseApprovalStatus>(query.ApprovalStatus);

        PagedResult<CylinderPurchase> result = await purchases.SearchAsync(
            tenantId, supplierId, approvalStatus, query.Page, query.PageSize, cancellationToken);

        List<CylinderPurchaseDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<CylinderPurchaseDto>(items, result.Page, result.PageSize, result.Total);
    }
}

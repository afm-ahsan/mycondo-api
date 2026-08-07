using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Queries.GetSuppliers;

public sealed class GetSuppliersQueryHandler(
    IGasCylinderSupplierRepository suppliers,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSuppliersQuery, PagedResult<GasCylinderSupplierDto>>
{
    public async ValueTask<PagedResult<GasCylinderSupplierDto>> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<GasCylinderSupplier> result = await suppliers.SearchAsync(tenantId, query.Page, query.PageSize, cancellationToken);

        List<GasCylinderSupplierDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<GasCylinderSupplierDto>(items, result.Page, result.PageSize, result.Total);
    }
}

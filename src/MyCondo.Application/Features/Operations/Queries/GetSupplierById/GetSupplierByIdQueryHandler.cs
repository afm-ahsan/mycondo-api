using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierById;

public sealed class GetSupplierByIdQueryHandler(
    IGasCylinderSupplierRepository suppliers,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSupplierByIdQuery, GasCylinderSupplierDto>
{
    public async ValueTask<GasCylinderSupplierDto> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GasCylinderSupplier supplier = await suppliers.GetByIdAsync(new GasCylinderSupplierId(query.GasCylinderSupplierId), cancellationToken)
            ?? throw new NotFoundException(nameof(GasCylinderSupplier), query.GasCylinderSupplierId);
        if (supplier.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GasCylinderSupplier), query.GasCylinderSupplierId);
        }

        return supplier.ToDto();
    }
}

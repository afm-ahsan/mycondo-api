using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateSupplier;

public sealed class DeactivateSupplierCommandHandler(
    IGasCylinderSupplierRepository suppliers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateSupplierCommandHandler> logger
) : IRequestHandler<DeactivateSupplierCommand, GasCylinderSupplierDto>
{
    public async ValueTask<GasCylinderSupplierDto> Handle(DeactivateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GasCylinderSupplierId id = new(command.GasCylinderSupplierId);
        GasCylinderSupplier supplier = await suppliers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(GasCylinderSupplier), command.GasCylinderSupplierId);
        if (supplier.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GasCylinderSupplier), command.GasCylinderSupplierId);
        }

        supplier.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Gas cylinder supplier {GasCylinderSupplierId} deactivated, tenant {TenantId}", id, tenantId);

        return supplier.ToDto();
    }
}

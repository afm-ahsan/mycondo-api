using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Commands.CreateSupplier;

public sealed class CreateSupplierCommandHandler(
    IGasCylinderSupplierRepository suppliers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateSupplierCommandHandler> logger
) : IRequestHandler<CreateSupplierCommand, GasCylinderSupplierDto>
{
    public async ValueTask<GasCylinderSupplierDto> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GasCylinderSupplier supplier = GasCylinderSupplier.Create(
            tenantId, command.Name, command.ContactPhone, command.ContactEmail, command.Address, clock.UtcNow);

        suppliers.Add(supplier);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Gas cylinder supplier {GasCylinderSupplierId} ('{Name}') created for tenant {TenantId}", supplier.Id, supplier.Name, tenantId);

        return supplier.ToDto();
    }
}

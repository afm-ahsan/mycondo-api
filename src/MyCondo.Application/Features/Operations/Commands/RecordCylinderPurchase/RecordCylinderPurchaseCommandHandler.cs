using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Commands.RecordCylinderPurchase;

public sealed class RecordCylinderPurchaseCommandHandler(
    ICylinderPurchaseRepository purchases,
    IGasCylinderSupplierRepository suppliers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordCylinderPurchaseCommandHandler> logger
) : IRequestHandler<RecordCylinderPurchaseCommand, CylinderPurchaseDto>
{
    public async ValueTask<CylinderPurchaseDto> Handle(RecordCylinderPurchaseCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GasCylinderSupplierId supplierId = new(command.SupplierId);
        GasCylinderSupplier supplier = await suppliers.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException(nameof(GasCylinderSupplier), command.SupplierId);
        if (supplier.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GasCylinderSupplier), command.SupplierId);
        }

        CylinderPurchase purchase = CylinderPurchase.Record(
            tenantId, supplierId, command.InvoiceNumber, command.PurchaseDate, command.CylinderType, command.Quantity,
            command.CylinderWeightKg, command.RatePerCylinder, command.DeliveryOrOtherCost, command.Remarks, clock.UtcNow);

        purchases.Add(purchase);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Cylinder purchase {CylinderPurchaseId} recorded for supplier {GasCylinderSupplierId}, tenant {TenantId}",
            purchase.Id, supplierId, tenantId);

        return purchase.ToDto();
    }
}

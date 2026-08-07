using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderPurchases;

namespace MyCondo.Application.Features.Operations.Commands.MarkCylinderPurchasePaid;

public sealed class MarkCylinderPurchasePaidCommandHandler(
    ICylinderPurchaseRepository purchases,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<MarkCylinderPurchasePaidCommandHandler> logger
) : IRequestHandler<MarkCylinderPurchasePaidCommand, CylinderPurchaseDto>
{
    public async ValueTask<CylinderPurchaseDto> Handle(MarkCylinderPurchasePaidCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        CylinderPurchaseId id = new(command.CylinderPurchaseId);
        CylinderPurchase purchase = await purchases.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CylinderPurchase), command.CylinderPurchaseId);
        if (purchase.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(CylinderPurchase), command.CylinderPurchaseId);
        }

        purchase.MarkPaid();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Cylinder purchase {CylinderPurchaseId} marked paid, tenant {TenantId}", id, tenantId);

        return purchase.ToDto();
    }
}

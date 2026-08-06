using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Application.Features.Utilities.Commands.DeactivateRatePlan;

public sealed class DeactivateRatePlanCommandHandler(
    IRatePlanRepository ratePlans,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateRatePlanCommandHandler> logger
) : IRequestHandler<DeactivateRatePlanCommand, RatePlanDto>
{
    public async ValueTask<RatePlanDto> Handle(DeactivateRatePlanCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        RatePlanId id = new(command.RatePlanId);
        RatePlan plan = await ratePlans.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(RatePlan), command.RatePlanId);
        if (plan.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(RatePlan), command.RatePlanId);
        }

        plan.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Rate plan {RatePlanId} deactivated, tenant {TenantId}", id, tenantId);

        IReadOnlyList<RateSlab> slabs = await ratePlans.GetSlabsForPlanAsync(id, cancellationToken);
        return plan.ToDto(slabs);
    }
}

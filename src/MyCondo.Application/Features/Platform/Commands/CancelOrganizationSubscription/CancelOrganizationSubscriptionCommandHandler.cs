using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.CancelOrganizationSubscription;

/// <summary>Active or PastDue → Canceled (ADR-032 Task 13D) — customer-initiated non-renewal, a manual
/// Platform Control Plane action; never inferred from billing/collection state (ADR-034 not implemented).</summary>
public sealed class CancelOrganizationSubscriptionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CancelOrganizationSubscriptionCommandHandler> logger
) : IRequestHandler<CancelOrganizationSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(CancelOrganizationSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        subscription.Cancel(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} canceled",
            tenant.Id, subscription.Id);

        return Unit.Value;
    }
}

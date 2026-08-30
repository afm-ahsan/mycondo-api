using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ReactivateOrganizationSubscription;

/// <summary>
/// PastDue → Active (ADR-032 Task 13D) — payment resolved before escalation to Restricted. Distinct from
/// <see cref="MyCondo.Application.Features.Platform.Commands.ReactivateOrganization.ReactivateOrganizationCommand"/>,
/// which operates on the separate Tenant/organization lifecycle axis, not this subscription axis.
/// </summary>
public sealed class ReactivateOrganizationSubscriptionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    IUnitOfWork unitOfWork,
    ILogger<ReactivateOrganizationSubscriptionCommandHandler> logger
) : IRequestHandler<ReactivateOrganizationSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(ReactivateOrganizationSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        subscription.Reactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} reactivated",
            tenant.Id, subscription.Id);

        return Unit.Value;
    }
}

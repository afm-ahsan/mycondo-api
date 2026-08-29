using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.RestrictOrganizationSubscription;

/// <summary>PastDue → Restricted (ADR-032 Task 13D) — escalated non-payment, past the grace window.</summary>
public sealed class RestrictOrganizationSubscriptionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RestrictOrganizationSubscriptionCommandHandler> logger
) : IRequestHandler<RestrictOrganizationSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(RestrictOrganizationSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        subscription.Restrict(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} restricted",
            tenant.Id, subscription.Id);

        return Unit.Value;
    }
}

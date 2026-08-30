using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ExpireOrganizationSubscription;

/// <summary>
/// Restricted or Canceled → Expired (ADR-032 Task 13D) — terminal. Must use
/// <see cref="IOrganizationSubscriptionRepository.GetLatestForTenantAsync"/>, not
/// <c>GetCurrentForTenantAsync</c>: "current" deliberately excludes Canceled subscriptions, but Canceled
/// is one of this transition's two valid source statuses.
/// </summary>
public sealed class ExpireOrganizationSubscriptionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ExpireOrganizationSubscriptionCommandHandler> logger
) : IRequestHandler<ExpireOrganizationSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(ExpireOrganizationSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        subscription.Expire(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} expired",
            tenant.Id, subscription.Id);

        return Unit.Value;
    }
}

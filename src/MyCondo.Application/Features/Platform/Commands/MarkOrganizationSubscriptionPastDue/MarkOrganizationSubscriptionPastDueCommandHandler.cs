using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.MarkOrganizationSubscriptionPastDue;

/// <summary>
/// Active → PastDue (ADR-032 Task 13D) — explicit Platform Control Plane action; ADR-034 billing
/// automation does not exist yet, so this transition is never inferred from unpaid invoices. Fetches via
/// <see cref="IOrganizationSubscriptionRepository.GetLatestForTenantAsync"/> (not
/// <c>GetCurrentForTenantAsync</c>) so a subscription already in a status this transition rejects still
/// surfaces the aggregate's own invalid-transition guard (422) instead of a misleading NotFound.
/// </summary>
public sealed class MarkOrganizationSubscriptionPastDueCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    IUnitOfWork unitOfWork,
    ILogger<MarkOrganizationSubscriptionPastDueCommandHandler> logger
) : IRequestHandler<MarkOrganizationSubscriptionPastDueCommand>
{
    public async ValueTask<Unit> Handle(MarkOrganizationSubscriptionPastDueCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        subscription.MarkPastDue();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} marked past due",
            tenant.Id, subscription.Id);

        return Unit.Value;
    }
}

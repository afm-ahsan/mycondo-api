using Mediator;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;

public sealed record ChangeOrganizationSubscriptionCommand(
    Guid OrganizationId,
    Guid SubscriptionPackageVersionId,
    BillingCycle BillingCycle,
    bool AutoRenew
) : IRequest;

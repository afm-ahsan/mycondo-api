using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ReactivateOrganizationSubscription;

public sealed record ReactivateOrganizationSubscriptionCommand(Guid OrganizationId) : IRequest;

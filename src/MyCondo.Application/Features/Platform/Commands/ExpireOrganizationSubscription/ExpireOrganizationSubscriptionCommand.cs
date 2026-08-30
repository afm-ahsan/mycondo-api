using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ExpireOrganizationSubscription;

public sealed record ExpireOrganizationSubscriptionCommand(Guid OrganizationId) : IRequest;

using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.CancelOrganizationSubscription;

public sealed record CancelOrganizationSubscriptionCommand(Guid OrganizationId) : IRequest;

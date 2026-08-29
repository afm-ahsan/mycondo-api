using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.RestrictOrganizationSubscription;

public sealed record RestrictOrganizationSubscriptionCommand(Guid OrganizationId) : IRequest;

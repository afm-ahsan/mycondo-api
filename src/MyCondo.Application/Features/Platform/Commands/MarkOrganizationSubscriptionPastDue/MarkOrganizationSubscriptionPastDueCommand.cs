using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.MarkOrganizationSubscriptionPastDue;

public sealed record MarkOrganizationSubscriptionPastDueCommand(Guid OrganizationId) : IRequest;

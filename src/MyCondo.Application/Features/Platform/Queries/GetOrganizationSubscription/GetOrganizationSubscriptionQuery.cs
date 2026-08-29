using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationSubscription;

public sealed record GetOrganizationSubscriptionQuery(Guid OrganizationId) : IRequest<OrganizationSubscriptionDto>;

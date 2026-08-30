using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionPackageOptions;

/// <summary>Lists the subscription packages/current-versions eligible for organization provisioning
/// selection (ADR-033 Task 13E.1). Takes no parameters — this is a platform-wide catalogue, not scoped
/// to any particular organization.</summary>
public sealed record GetSubscriptionPackageOptionsQuery : IRequest<List<SubscriptionPackageOptionDto>>;

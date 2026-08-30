using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Subscription.DTOs;

namespace MyCondo.Application.Features.Subscription.Queries.GetTenantBillingResolution;

/// <summary>
/// Tenant-safe CondoBD SaaS billing-resolution read (ADR-032/ADR-034 Task 14I) — the organization is
/// always resolved from the authenticated tenant context (<see cref="ICurrentUserProvider.TenantId"/>),
/// never from a request parameter, so there is no way to ask about another organization's billing.
/// Marked <see cref="IBillingResolutionOperation"/> (not <see cref="ILifecycleReadOperation"/>) — this is
/// the one operation ADR-032 §5's Restricted/Expired/Canceled read-only mode deliberately carves out
/// specifically for CondoBD subscription billing resolution, not an ordinary tenant-application read.
/// </summary>
public sealed record GetTenantBillingResolutionQuery : IRequest<TenantBillingResolutionDto>, IBillingResolutionOperation;

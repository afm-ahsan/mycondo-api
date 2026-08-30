using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Subscription.DTOs;
using MyCondo.Application.Features.Subscription.Queries.GetTenantBillingResolution;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Tenant-facing CondoBD SaaS billing-resolution read (ADR-032/ADR-034 Task 14I) — narrow tenant
/// counterpart to the Platform Control Plane's <c>PlatformBillingEndpoints</c>, never exposed to tenant
/// callers. Gated by <c>user.view</c> — the same "administration-adjacent" permission
/// <c>/admin/users</c>/<c>/admin/roles</c> already gate on tenant-side, held only by a tenant's
/// OrganizationAdmin (blanket grant) and the Auditor role, never by an ordinary resident/staff role in
/// the default catalogue (ADR-034 Task 14I §5 — no new permission was needed).
/// </summary>
public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/subscription").WithTags("Subscription");

        group.MapGet("/billing-resolution", async (ISender sender, CancellationToken ct) =>
            {
                TenantBillingResolutionDto result = await sender.Send(new GetTenantBillingResolutionQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.view")
            .Produces<TenantBillingResolutionDto>(StatusCodes.Status200OK);

        return app;
    }
}

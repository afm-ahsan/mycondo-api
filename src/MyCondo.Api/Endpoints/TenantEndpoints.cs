using Mediator;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// SaaS-tenant (organization) lifecycle management — provisioning, activation, and suspension — lives
/// exclusively under the isolated Platform Administration surface
/// (<see cref="PlatformOrganizationEndpoints"/>, gated by <c>platform.organization.*</c> via
/// <c>RequirePlatformPermission</c>). This group used to also expose Provision/Activate/Suspend here,
/// gated by the ordinary tenant-scoped <c>tenant.manage</c> permission — a latent cross-boundary
/// escalation path, since <see cref="MyCondo.Application.Common.Services.OrganizationAdminBootstrapper"/>
/// grants every non-platform-module permission (including <c>tenant.manage</c>) to a tenant's own
/// OrganizationAdmin by default. Removed as part of the Administration/Owner/Expense Management audit
/// (mycondo-docs Create Tenant audit decision) — the Platform endpoints are the only supported path
/// now. Only the anonymous slug lookup remains here, since the login flow needs it before any tenant
/// context exists.
/// </summary>
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/tenants").WithTags("Tenants");

        // Anonymous: the frontend resolves a slug to a tenant before showing the login form.
        group.MapGet("/by-slug/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
            {
                TenantSummaryDto result = await sender.Send(new GetTenantBySlugQuery(slug), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .Produces<TenantSummaryDto>(StatusCodes.Status200OK);

        return app;
    }
}

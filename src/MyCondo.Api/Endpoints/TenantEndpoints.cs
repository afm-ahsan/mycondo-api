using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;
using MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;
using MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

namespace MyCondo.Api.Endpoints;

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
            .AllowAnonymous();

        // Gated by tenant.manage. As of this slice, no user can hold that permission yet — the
        // permission catalogue hasn't been seeded (MASTER_BACKLOG.md ID-2). This endpoint establishes
        // the correct shape now rather than opening it anonymously in the meantime; the local dev
        // bootstrap path is MyCondo.Infrastructure.Seed.DevelopmentTenantSeeder, not this endpoint.
        group.MapPost("/", async (ProvisionTenantCommand command, ISender sender, CancellationToken ct) =>
            {
                ProvisionTenantResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("tenant.manage");

        group.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivateTenantCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("tenant.manage");

        group.MapPost("/{id:guid}/suspend", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SuspendTenantCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("tenant.manage");

        return app;
    }
}

using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationById;
using MyCondo.Application.Features.Platform.Queries.ListOrganizations;
using MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;
using MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;
using MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope organization (Tenant) administration — Phase 1 metadata operations only. Reuses the
/// EXISTING ProvisionTenant/SuspendTenant/ActivateTenant Application-layer commands verbatim (same
/// domain calls the tenant-side /api/v1/tenants endpoints already make) rather than duplicating
/// business logic; only the Api-layer route, authentication scheme, and permission boundary differ.
/// See mycondo-docs ADR-019 and the approved Phase 1 blueprint §7/§12.
///
/// Deliberately does NOT expose "reactivate" (Suspended -> Active) or "update": Tenant.Activate()
/// today explicitly rejects that transition (see Tenant.cs), and there is no domain method for
/// renaming/updating a Tenant. Inventing either would be a tenant-lifecycle domain change, which is
/// out of scope for Phase 1 — see the Phase 1 completion report's "Explicitly deferred operations"
/// section. The platform.organization.reactivate and platform.organization.update permission codes
/// are still seeded (for forward compatibility) but no endpoint checks them yet.
/// </summary>
public static class PlatformOrganizationEndpoints
{
    public static IEndpointRouteBuilder MapPlatformOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/platform/organizations").WithTags("Platform Organizations");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<TenantSummaryDto> result = await sender.Send(new ListOrganizationsQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.read")
            .Produces<IReadOnlyList<TenantSummaryDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                TenantSummaryDto result = await sender.Send(new GetOrganizationByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.read")
            .Produces<TenantSummaryDto>(StatusCodes.Status200OK);

        group.MapPost("/", async (
                ProvisionTenantCommand command,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                ProvisionTenantResult result = await sender.Send(command, ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.created",
                    targetType: "Tenant",
                    targetId: result.TenantId.ToString(),
                    tenantId: result.TenantId));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.create")
            .Produces<ProvisionTenantResult>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/suspend", async (
                Guid id,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                await sender.Send(new SuspendTenantCommand(id), ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.suspended",
                    targetType: "Tenant",
                    targetId: id.ToString(),
                    tenantId: id));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.organization.suspend")
            .Produces(StatusCodes.Status204NoContent);

        // Not one of the blueprint's illustrative permission codes, but required plumbing: without it,
        // "create organization" alone would leave every new organization permanently stuck in
        // PendingActivation with no reachable way to activate it (the existing tenant-side
        // /api/v1/tenants/{id}/activate endpoint requires tenant.manage, which nothing is seeded to
        // hold). This reuses the existing, already-safe ActivateTenantCommand/Tenant.Activate() domain
        // behavior verbatim — it is wiring, not new domain logic. See the Phase 1 completion report.
        group.MapPost("/{id:guid}/activate", async (
                Guid id,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                await sender.Send(new ActivateTenantCommand(id), ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.activated",
                    targetType: "Tenant",
                    targetId: id.ToString(),
                    tenantId: id));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.organization.activate")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Commands.ReactivateOrganization;
using MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;
using MyCondo.Application.Features.Platform.Commands.UpdateOrganization;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationById;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationSummaryStats;
using MyCondo.Application.Features.Platform.Queries.ListOrganizations;
using MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;
using MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformAudit;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope organization (Tenant) administration. Organization creation now provisions the
/// tenant AND its founding administrator atomically (<see cref="ProvisionOrganizationWithAdminCommand"/>)
/// rather than leaving a new organization stuck in PendingActivation with no admin until someone
/// self-registers against it. Suspend/Activate reuse the existing tenant-lifecycle domain commands
/// verbatim; Reactivate/Update/Modules are new, first-time wiring for permissions that were seeded in
/// Phase 1 but never implemented. See mycondo-docs ADR-019 for the Platform/tenant isolation model
/// this endpoint group must preserve.
/// </summary>
public static class PlatformOrganizationEndpoints
{
    public static IEndpointRouteBuilder MapPlatformOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/platform/organizations").WithTags("Platform Organizations");

        group.MapGet("/", async (
                int page, int pageSize, string? search, string? status, ISender sender, CancellationToken ct) =>
            {
                PagedResult<OrganizationListItemDto> result = await sender.Send(
                    new ListOrganizationsQuery(page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize, search, status),
                    ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.read")
            .Produces<PagedResult<OrganizationListItemDto>>(StatusCodes.Status200OK);

        group.MapGet("/stats", async (ISender sender, CancellationToken ct) =>
            {
                OrganizationSummaryStatsDto result = await sender.Send(new GetOrganizationSummaryStatsQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.read")
            .Produces<OrganizationSummaryStatsDto>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                OrganizationDetailDto result = await sender.Send(new GetOrganizationByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.organization.read")
            .Produces<OrganizationDetailDto>(StatusCodes.Status200OK);

        group.MapPost("/", async (
                ProvisionOrganizationWithAdminCommand command,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                ProvisionOrganizationResult result = await sender.Send(command, ct);

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
            .Produces<ProvisionOrganizationResult>(StatusCodes.Status200OK);

        group.MapPatch("/{id:guid}", async (
                Guid id,
                UpdateOrganizationRequest request,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                await sender.Send(new UpdateOrganizationCommand(id, request.Name, request.Code), ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.updated",
                    targetType: "Tenant",
                    targetId: id.ToString(),
                    tenantId: id));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.organization.update")
            .Produces(StatusCodes.Status204NoContent);

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

        group.MapPost("/{id:guid}/reactivate", async (
                Guid id,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                await sender.Send(new ReactivateOrganizationCommand(id), ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.reactivated",
                    targetType: "Tenant",
                    targetId: id.ToString(),
                    tenantId: id));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.organization.reactivate")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/modules", async (
                Guid id,
                ReplaceOrganizationModulesRequest request,
                ISender sender,
                ICurrentPlatformUserProvider currentUser,
                IPlatformAuditLogRepository auditLog,
                IUnitOfWork unitOfWork,
                IClock clock,
                CancellationToken ct) =>
            {
                await sender.Send(new ReplaceOrganizationModulesCommand(id, request.ModuleKeys), ct);

                auditLog.Add(PlatformAuditLogEntry.Record(
                    clock.UtcNow,
                    actorPlatformUserId: currentUser.PlatformUserId,
                    action: "platform.organization.modules.replaced",
                    targetType: "Tenant",
                    targetId: id.ToString(),
                    tenantId: id));
                await unitOfWork.SaveChangesAsync(ct);

                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.organization.features.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record UpdateOrganizationRequest(string Name, string? Code);

public sealed record ReplaceOrganizationModulesRequest(IReadOnlyList<string> ModuleKeys);

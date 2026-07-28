using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Roles.Commands.AssignRoleToUser;
using MyCondo.Application.Features.Roles.Commands.CreateRole;
using MyCondo.Application.Features.Roles.Commands.DeactivateRole;
using MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;
using MyCondo.Application.Features.Roles.Commands.RemovePermissionFromRole;
using MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;

namespace MyCondo.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder roles = app.MapGroup("/api/v1/roles").WithTags("Roles");

        roles.MapPost("/", async (CreateRoleCommand command, ISender sender, CancellationToken ct) =>
            {
                CreateRoleResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("role.manage");

        roles.MapPost("/{id:guid}/permissions", async (Guid id, GrantPermissionToRoleRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new GrantPermissionToRoleCommand(id, body.PermissionId), ct);
                return Results.NoContent();
            })
            .RequirePermission("role.manage");

        roles.MapPost("/{id:guid}/assignments", async (Guid id, AssignRoleToUserRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignRoleToUserCommand(id, body.UserId, body.BuildingId), ct);
                return Results.NoContent();
            })
            .RequirePermission("role.manage");

        roles.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                List<RoleSummaryDto> result = await sender.Send(new GetRolesForTenantQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("role.view");

        roles.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateRoleCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("role.manage");

        roles.MapDelete("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemovePermissionFromRoleCommand(id, permissionId), ct);
                return Results.NoContent();
            })
            .RequirePermission("role.manage");

        roles.MapDelete("/{id:guid}/assignments/{userId:guid}", async (Guid id, Guid userId, Guid? buildingId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RevokeRoleFromUserCommand(id, userId, buildingId), ct);
                return Results.NoContent();
            })
            .RequirePermission("role.manage");

        app.MapGet("/api/v1/permissions", async (ISender sender, CancellationToken ct) =>
            {
                List<PermissionDto> result = await sender.Send(new GetPermissionCatalogueQuery(), ct);
                return Results.Ok(result);
            })
            .WithTags("Roles")
            .RequirePermission("permission.view");

        return app;
    }
}

public sealed record GrantPermissionToRoleRequest(Guid PermissionId);

public sealed record AssignRoleToUserRequest(Guid UserId, Guid? BuildingId);

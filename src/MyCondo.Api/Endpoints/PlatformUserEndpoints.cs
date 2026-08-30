using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Platform.Users.Commands.ActivatePlatformUser;
using MyCondo.Application.Features.Platform.Users.Commands.AssignPlatformRoleToUser;
using MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;
using MyCondo.Application.Features.Platform.Users.Commands.DeactivatePlatformUser;
using MyCondo.Application.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;
using MyCondo.Application.Features.Platform.Users.Commands.UpdatePlatformUser;
using MyCondo.Application.Features.Platform.Users.DTOs;
using MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserById;
using MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserRoleAssignments;
using MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUsers;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope operator account administration (mycondo-docs ADR-035). Structurally mirrors
/// <see cref="UserEndpoints"/>/<see cref="RoleEndpoints"/> but every gate is
/// <see cref="EndpointRequirePlatformPermissionExtensions.RequirePlatformPermission"/>. Role
/// assign/revoke is gated at the same coarse action-permission granularity <see cref="RoleEndpoints"/>
/// uses for its own assign/revoke endpoints (a single "manage" permission covering any role) — the
/// finer <c>platform.user.manageSuperAdmins</c> check for a SuperAdmin-role grant/revoke is enforced
/// as defense-in-depth inside the command handlers themselves, not by inventing a stricter endpoint
/// gate than the tenant precedent.
/// </summary>
public static class PlatformUserEndpoints
{
    public static IEndpointRouteBuilder MapPlatformUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder users = app.MapGroup("/api/v1/platform/users").WithTags("Platform Users");

        users.MapGet("/", async (
                string? searchText, PlatformUserStatus? status, int? page, int? pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<PlatformUserSummaryDto> result = await sender.Send(
                    new GetPlatformUsersQuery(
                        searchText, status,
                        page is null or < 1 ? 1 : page.Value,
                        pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.user.view")
            .Produces<PagedResult<PlatformUserSummaryDto>>(StatusCodes.Status200OK);

        users.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                PlatformUserDetailDto result = await sender.Send(new GetPlatformUserByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.user.view")
            .Produces<PlatformUserDetailDto>(StatusCodes.Status200OK);

        users.MapGet("/{id:guid}/roles", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                List<PlatformUserRoleAssignmentDto> result = await sender.Send(
                    new GetPlatformUserRoleAssignmentsQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.user.view")
            .Produces<List<PlatformUserRoleAssignmentDto>>(StatusCodes.Status200OK);

        users.MapPost("/", async (CreatePlatformUserCommand command, ISender sender, CancellationToken ct) =>
            {
                CreatePlatformUserResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.user.create")
            .Produces<CreatePlatformUserResult>(StatusCodes.Status200OK);

        users.MapPut("/{id:guid}", async (
                Guid id, UpdatePlatformUserRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdatePlatformUserCommand(id, body.DisplayName), ct);
                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.user.update")
            .Produces(StatusCodes.Status204NoContent);

        users.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePlatformUserCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.user.deactivate")
            .Produces(StatusCodes.Status204NoContent);

        users.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivatePlatformUserCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.user.deactivate")
            .Produces(StatusCodes.Status204NoContent);

        users.MapPost("/{id:guid}/roles", async (
                Guid id, AssignPlatformRoleToUserRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignPlatformRoleToUserCommand(body.RoleId, id), ct);
                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.user.update")
            .Produces(StatusCodes.Status204NoContent);

        users.MapDelete("/{id:guid}/roles/{roleId:guid}", async (
                Guid id, Guid roleId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RevokePlatformRoleFromUserCommand(roleId, id), ct);
                return Results.NoContent();
            })
            .RequirePlatformPermission("platform.user.update")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record UpdatePlatformUserRequest(string DisplayName);

public sealed record AssignPlatformRoleToUserRequest(Guid RoleId);

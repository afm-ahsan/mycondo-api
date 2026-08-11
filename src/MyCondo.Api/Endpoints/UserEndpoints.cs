using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Users.Commands.CreateUser;
using MyCondo.Application.Features.Users.Commands.DeactivateUser;
using MyCondo.Application.Features.Users.Commands.EnableUser;
using MyCondo.Application.Features.Users.Commands.UpdateUser;
using MyCondo.Application.Features.Users.Queries.GetUserById;
using MyCondo.Application.Features.Users.Queries.GetUserRoleAssignments;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder users = app.MapGroup("/api/v1/users").WithTags("Users");

        users.MapGet("/", async (
                string? searchText, Guid? roleId, bool? isActive, int? page, int? pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<UserSummaryDto> result = await sender.Send(
                    new GetUsersForTenantQuery(
                        searchText, roleId, isActive,
                        page is null or < 1 ? 1 : page.Value,
                        pageSize is null or < 1 ? 20 : pageSize.Value),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.view")
            .Produces<PagedResult<UserSummaryDto>>(StatusCodes.Status200OK);

        users.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                UserDetailDto result = await sender.Send(new GetUserByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.view")
            .Produces<UserDetailDto>(StatusCodes.Status200OK);

        users.MapGet("/{id:guid}/roles", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                List<UserRoleAssignmentDto> result = await sender.Send(new GetUserRoleAssignmentsQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.view")
            .Produces<List<UserRoleAssignmentDto>>(StatusCodes.Status200OK);

        users.MapPost("/", async (CreateUserCommand command, ISender sender, CancellationToken ct) =>
            {
                CreateUserResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.create")
            .Produces<CreateUserResult>(StatusCodes.Status200OK);

        users.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateUserCommand(id, body.FullName, body.PhoneNumber), ct);
                return Results.NoContent();
            })
            .RequirePermission("user.update")
            .Produces(StatusCodes.Status204NoContent);

        users.MapPost("/{id:guid}/disable", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateUserCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("user.disable")
            .Produces(StatusCodes.Status204NoContent);

        users.MapPost("/{id:guid}/enable", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new EnableUserCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("user.disable")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record UpdateUserRequest(string FullName, string? PhoneNumber);

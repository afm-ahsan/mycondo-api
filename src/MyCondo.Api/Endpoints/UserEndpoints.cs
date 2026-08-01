using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Users.Commands.DeactivateUser;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

namespace MyCondo.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder users = app.MapGroup("/api/v1/users").WithTags("Users");

        users.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                List<UserSummaryDto> result = await sender.Send(new GetUsersForTenantQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("user.view")
            .Produces<List<UserSummaryDto>>(StatusCodes.Status200OK);

        users.MapPost("/{id:guid}/disable", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateUserCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("user.disable")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Platform.Roles.DTOs;
using MyCondo.Application.Features.Platform.Roles.Queries.GetPlatformRoles;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Read-only platform-role directory backing the Platform Users role-assignment UI (mycondo-docs
/// ADR-036) — see <see cref="GetPlatformRolesQuery"/>'s doc comment for why this exists as its own
/// minimal endpoint rather than full platform-role CRUD.
/// </summary>
public static class PlatformRoleEndpoints
{
    public static IEndpointRouteBuilder MapPlatformRoleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder roles = app.MapGroup("/api/v1/platform/roles").WithTags("Platform Roles");

        roles.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                List<PlatformRoleSummaryDto> result = await sender.Send(new GetPlatformRolesQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.user.view")
            .Produces<List<PlatformRoleSummaryDto>>(StatusCodes.Status200OK);

        return app;
    }
}

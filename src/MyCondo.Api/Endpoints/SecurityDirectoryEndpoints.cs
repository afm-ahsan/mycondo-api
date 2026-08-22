using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectory;
using MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// The merged, security-facing resident directory (Owner via <c>FlatOwnership</c> + Tenant via
/// <c>OccupancyRegistration</c>) — supersedes the retired <c>/api/v1/occupancy-registrations/security</c>
/// routes, which only ever covered Tenants. A single base permission
/// (<c>security.directory.view</c>) gates the endpoints; which optional detail sections come back is
/// decided per-request inside <c>GetSecurityDirectoryDetailQueryHandler</c> based on the caller's
/// granular <c>security.directory.*</c> grants.
/// </summary>
public static class SecurityDirectoryEndpoints
{
    public static IEndpointRouteBuilder MapSecurityDirectoryEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder directory = app.MapGroup("/api/v1/security/directory").WithTags("Security Directory");

        directory.MapGet("/", async (
                string? search, Guid? buildingId, Guid? flatId, string? accessStatus, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<SecurityDirectoryEntryDto> result = await sender.Send(
                    new GetSecurityDirectoryQuery(
                        search, buildingId, flatId, accessStatus, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("security.directory.view")
            .Produces<PagedResult<SecurityDirectoryEntryDto>>(StatusCodes.Status200OK);

        directory.MapGet("/{id:guid}", async (Guid id, string type, ISender sender, CancellationToken ct) =>
            {
                SecurityDirectoryDetailDto result = await sender.Send(new GetSecurityDirectoryDetailQuery(id, type), ct);
                return Results.Ok(result);
            })
            .RequirePermission("security.directory.view")
            .Produces<SecurityDirectoryDetailDto>(StatusCodes.Status200OK);

        return app;
    }
}

using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authorization;

/// <summary>
/// Denies access with 403 unless the authenticated caller holds <paramref name="permission"/> either
/// tenant-wide, or scoped to the building identified by the <paramref name="buildingIdParameterName"/>
/// route or query parameter. Mirrors <see cref="PermissionEndpointFilter"/> — see ADR-014 for the
/// `perm`/`bperm` claims split this depends on.
///
/// No endpoint uses this yet: every permission the catalogue marks IsBuildingScopable belongs to a
/// module (property/resident/billing/complaint/etc.) that hasn't shipped an HTTP endpoint yet — this
/// filter exists so the first one that does can call
/// <see cref="EndpointRequireBuildingPermissionExtensions.RequireBuildingPermission"/> instead of
/// hand-rolling the check.
/// </summary>
public sealed class BuildingScopedPermissionEndpointFilter(string permission, string buildingIdParameterName)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ICurrentUserProvider currentUser =
            context.HttpContext.RequestServices.GetRequiredService<ICurrentUserProvider>();

        Guid? buildingId = ResolveBuildingId(context.HttpContext, buildingIdParameterName);

        if (!currentUser.HasPermissionForBuilding(permission, buildingId))
        {
            return Results.Forbid();
        }

        return await next(context);
    }

    private static Guid? ResolveBuildingId(HttpContext httpContext, string parameterName)
    {
        if (httpContext.Request.RouteValues.TryGetValue(parameterName, out object? routeValue)
            && Guid.TryParse(routeValue?.ToString(), out Guid fromRoute))
        {
            return fromRoute;
        }

        if (httpContext.Request.Query.TryGetValue(parameterName, out Microsoft.Extensions.Primitives.StringValues queryValue)
            && Guid.TryParse(queryValue, out Guid fromQuery))
        {
            return fromQuery;
        }

        return null;
    }
}

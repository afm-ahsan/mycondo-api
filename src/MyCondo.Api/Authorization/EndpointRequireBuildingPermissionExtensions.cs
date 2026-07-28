namespace MyCondo.Api.Authorization;

public static class EndpointRequireBuildingPermissionExtensions
{
    /// <summary>
    /// Requires the caller to be authenticated and hold <paramref name="permission"/>, either
    /// tenant-wide or scoped to the building named by <paramref name="buildingIdParameterName"/>
    /// (checked as a route value first, then a query parameter). Use this instead of
    /// <see cref="EndpointRequirePermissionExtensions.RequirePermission"/> for any endpoint whose
    /// permission is marked <c>IsBuildingScopable</c> in the permission catalogue.
    /// </summary>
    public static RouteHandlerBuilder RequireBuildingPermission(
        this RouteHandlerBuilder builder, string permission, string buildingIdParameterName = "buildingId") =>
        builder
            .RequireAuthorization()
            .AddEndpointFilter(new BuildingScopedPermissionEndpointFilter(permission, buildingIdParameterName));
}

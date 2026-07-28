namespace MyCondo.Api.Authorization;

public static class EndpointRequirePermissionExtensions
{
    /// <summary>
    /// Requires the caller to be authenticated and hold <paramref name="permission"/>. Every endpoint
    /// must declare either this or <c>.AllowAnonymous()</c> explicitly — see
    /// mycondo-api/.claude/skills/api-contracts.md.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder
            .RequireAuthorization()
            .AddEndpointFilter(new PermissionEndpointFilter(permission));
}

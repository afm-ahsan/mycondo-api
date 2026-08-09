using MyCondo.Api.Authentication;

namespace MyCondo.Api.Authorization;

public static class EndpointRequirePlatformPermissionExtensions
{
    /// <summary>
    /// Requires the caller to be authenticated via the "Platform" scheme (see
    /// <see cref="PlatformAuthenticationDefaults"/>) and hold <paramref name="permission"/>. This is
    /// the ONLY way a Platform-scope endpoint should declare its authorization boundary — it enforces
    /// scheme isolation first (a tenant token is rejected before this filter ever runs) and the
    /// explicit permission second. See mycondo-docs ADR-019.
    /// </summary>
    public static RouteHandlerBuilder RequirePlatformPermission(this RouteHandlerBuilder builder, string permission) =>
        builder
            .RequireAuthorization(PlatformAuthenticationDefaults.AuthorizationPolicyName)
            .AddEndpointFilter(new PlatformPermissionEndpointFilter(permission));
}

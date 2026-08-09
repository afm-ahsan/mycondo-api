namespace MyCondo.Api.Authentication;

public static class PlatformAuthenticationDefaults
{
    /// <summary>
    /// The named JWT bearer scheme validated against <c>Jwt:PlatformAudience</c>. Deliberately never
    /// registered as the default authentication scheme (see <see cref="JwtBearerSetup"/>) — every
    /// platform endpoint must explicitly opt into it via
    /// <see cref="MyCondo.Api.Authorization.EndpointRequirePlatformPermissionExtensions.RequirePlatformPermission"/>,
    /// and every other endpoint keeps using the tenant scheme's implicit default, unchanged.
    /// </summary>
    public const string SchemeName = "Platform";

    public const string AuthorizationPolicyName = "Platform";
}

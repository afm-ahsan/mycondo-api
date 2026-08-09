using System.Security.Claims;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

/// <summary>
/// Reads <see cref="HttpContext.User"/> exactly like <see cref="CurrentUserProvider"/> does — safe
/// because ASP.NET Core's authorization middleware re-authenticates and replaces
/// <see cref="HttpContext.User"/> with the Platform scheme's principal for any endpoint whose policy
/// explicitly requires it (see <see cref="MyCondo.Api.Authorization.EndpointRequirePlatformPermissionExtensions"/>),
/// so this provider never sees a mix of tenant and platform claims.
/// </summary>
public sealed class PlatformCurrentUserProvider(IHttpContextAccessor http) : ICurrentPlatformUserProvider
{
    private const string PermissionClaim = "perm";

    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? PlatformUserId
    {
        get
        {
            string? sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out Guid id) ? id : null;
        }
    }

    public bool HasPermission(string permission) =>
        Principal?.Claims.Any(c =>
            string.Equals(c.Type, PermissionClaim, StringComparison.Ordinal)
            && string.Equals(c.Value, permission, StringComparison.Ordinal)) ?? false;
}

namespace MyCondo.Api.Authentication;

/// <summary>
/// Platform-scope analogue of <see cref="RefreshTokenCookie"/> — a physically distinct cookie
/// (different name, different path) so a browser holding both a tenant session and a platform session
/// can never have one overwrite or leak into the other.
/// </summary>
public static class PlatformRefreshTokenCookie
{
    public const string CookieName = "mycondo_platform_rt";

    public static void Append(HttpContext http, string token, DateTimeOffset expiresAtUtc)
    {
        http.Response.Cookies.Append(CookieName, token, BuildOptions(http, expiresAtUtc));
    }

    public static void Clear(HttpContext http)
    {
        http.Response.Cookies.Delete(CookieName, BuildOptions(http, DateTimeOffset.UnixEpoch));
    }

    public static string? Read(HttpContext http) =>
        http.Request.Cookies.TryGetValue(CookieName, out string? value) ? value : null;

    private static CookieOptions BuildOptions(HttpContext http, DateTimeOffset expiresAtUtc) =>
        new()
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/platform/auth",
            Expires = expiresAtUtc,
        };
}

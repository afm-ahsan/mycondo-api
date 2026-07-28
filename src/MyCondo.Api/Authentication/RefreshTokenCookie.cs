namespace MyCondo.Api.Authentication;

/// <summary>
/// The refresh token lives in an HttpOnly cookie, never in a JSON response body — CLAUDE.md's rule
/// for both repos ("Refresh token in HTTP-only cookie; access token in memory"). Login/Register/Refresh
/// call <see cref="Append"/> after issuing a token pair; Refresh/Logout call <see cref="Read"/> instead
/// of binding a refresh token from the request body.
/// </summary>
public static class RefreshTokenCookie
{
    public const string CookieName = "mycondo_rt";

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
            // Matches the incoming request's scheme — a hardcoded `Secure = true` would silently drop
            // the cookie on plain HTTP local dev (the API runs on http://localhost:5000, not https).
            Secure = http.Request.IsHttps,
            // The frontend (:5173) and backend (:5000) are different origins but the same site
            // (both "localhost") — SameSite=Strict still allows the cookie on same-site fetch/XHR,
            // it only blocks genuinely cross-site requests.
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = expiresAtUtc,
        };
}

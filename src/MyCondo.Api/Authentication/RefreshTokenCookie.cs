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
            // the cookie if the API is ever hit over plain HTTP local dev (it runs HTTPS-primary on
            // https://localhost:7219, with an http://localhost:5219 fallback — see
            // docs/local-development-ports.md).
            Secure = http.Request.IsHttps,
            // The frontend (:4219) and backend (:7219/:5219) are different origins but can still be
            // same-site (both "localhost") — SameSite=Strict allows the cookie on same-site fetch/XHR,
            // it only blocks genuinely cross-site requests. Same-site classification is *schemeful*
            // (Chrome/Firefox/Safari): http and https on the same host count as different sites. The
            // frontend dev server is plain HTTP, so it must talk to the HTTP fallback port (5219), not
            // the HTTPS port (7219), for this cookie to round-trip locally — see mycondo-web/.env.
            // Production is unaffected: app.mycondo.app and api.mycondo.app both use https.
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = expiresAtUtc,
        };
}

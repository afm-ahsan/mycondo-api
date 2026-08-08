using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

/// <summary>
/// Deliberately reads only <see cref="HttpContext.Connection"/>'s <c>RemoteIpAddress</c> — never
/// re-parses <c>X-Forwarded-For</c> itself. <c>ForwardedHeadersMiddleware</c> (see
/// <c>app.UseForwardedHeaders()</c> in Program.cs, configured by
/// <c>DependencyInjection.AddForwardedHeadersForTrustedProxy</c>) already rewrites
/// <c>Connection.RemoteIpAddress</c> from that header, but only when the request actually came through
/// a configured, trusted proxy. Parsing the raw header here directly — the previous implementation —
/// meant any caller could set an arbitrary `X-Forwarded-For` value and have it accepted verbatim into
/// this app's own auth audit trail (Login/Register/RefreshToken/Logout all record this value), with no
/// proxy-trust check at all. Reading only the post-middleware, trust-validated value closes that.
/// </summary>
public sealed class HttpRequestIpAccessor(IHttpContextAccessor http) : IRequestIpAccessor
{
    public string IpAddress => http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

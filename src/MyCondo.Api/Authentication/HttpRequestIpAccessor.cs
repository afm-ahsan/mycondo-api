using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

public sealed class HttpRequestIpAccessor(IHttpContextAccessor http) : IRequestIpAccessor
{
    public string IpAddress
    {
        get
        {
            HttpContext? ctx = http.HttpContext;
            if (ctx is null)
            {
                return "unknown";
            }

            // Honour reverse-proxy headers if present (set by the load balancer).
            string? forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }

            return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}

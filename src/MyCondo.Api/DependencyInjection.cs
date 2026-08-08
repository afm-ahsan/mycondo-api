using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Json;
using MyCondo.Api.Authentication;
using MyCondo.Api.HealthChecks;
using MyCondo.Api.Observability;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<IRequestIpAccessor, HttpRequestIpAccessor>();

        services.AddForwardedHeadersForTrustedProxy(configuration);

        services.AddProblemDetails();

        services.Configure<JsonOptions>(opts =>
        {
            opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddOpenApi();
        services.AddJwtAuthentication(configuration);
        services.AddMyCondoHealthChecks(configuration);
        services.AddMyCondoOpenTelemetry(configuration);

        services.AddCors(options =>
        {
            string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4219"];

            options.AddPolicy("DefaultCors", builder =>
            {
                builder
                    .WithOrigins(origins)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("X-Correlation-Id");
            });
        });

        services.AddRateLimiter(opt =>
        {
            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opt.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter
                .Create<HttpContext, string>(ctx =>
                {
                    string partitionKey = ctx.User.Identity?.Name
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "anon";

                    return System.Threading.RateLimiting.RateLimitPartition
                        .GetTokenBucketLimiter(
                            partitionKey,
                            _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 100,
                                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                                TokensPerPeriod = 50,
                                AutoReplenishment = true,
                                QueueLimit = 0
                            });
                });

            // Stricter, dedicated policy for unauthenticated credential-entry endpoints
            // (login/register) — the global limiter above is a generous ~300+ req/min sustained
            // per key, a weak brute-force deterrent on its own. 10 attempts/minute per client IP
            // is tight enough to slow credential stuffing/brute force but loose enough that a
            // real user mistyping a password a few times in one session is never blocked — see
            // UX-6 production-hardening discovery. Partitions by IP only (not by attempted
            // username/email), since the whole point is to limit an anonymous caller regardless
            // of which account(s) they're probing.
            opt.AddPolicy("auth", ctx =>
            {
                string partitionKey = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";

                return System.Threading.RateLimiting.RateLimitPartition
                    .GetFixedWindowLimiter(
                        partitionKey,
                        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        });
            });
        });

        return services;
    }

    /// <summary>
    /// Configures <c>ForwardedHeadersMiddleware</c> so that, once <c>app.UseForwardedHeaders()</c> runs
    /// early in the pipeline (see Program.cs), <c>HttpContext.Connection.RemoteIpAddress</c> and
    /// <c>HttpContext.Request.Scheme</c> reflect the real client — not the reverse proxy's own
    /// connection — everywhere downstream: HTTPS redirection, the refresh-token cookie's `Secure` flag
    /// (<see cref="RefreshTokenCookie"/>), and IP-partitioned rate limiting all depend on this being
    /// correct. See UX-6 production-hardening discovery for the exact defect this closes.
    ///
    /// Deliberately does NOT default to trusting every proxy: ASP.NET Core's own default
    /// (<c>KnownProxies</c>/<c>KnownNetworks</c> = loopback only) is left in place unless
    /// <c>ForwardedHeaders:KnownProxies</c> (comma-separated IPs) or <c>ForwardedHeaders:KnownNetworks</c>
    /// (comma-separated CIDR, e.g. "10.0.0.0/8") is explicitly configured — this repo's production
    /// deployment topology (which load balancer/ingress, its address range) isn't decided yet (see
    /// mycondo-docs/kickoff.md and the CORS-origin gap noted alongside this same finding). Until ops
    /// sets one of those two settings for the real environment, forwarded headers from anything other
    /// than loopback are correctly ignored — the safe default — rather than trusted blindly.
    /// </summary>
    public static IServiceCollection AddForwardedHeadersForTrustedProxy(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            string[] knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
            foreach (string proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy.Trim(), out IPAddress? address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            string[] knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
            foreach (string network in knownNetworks)
            {
                if (System.Net.IPNetwork.TryParse(network.Trim(), out System.Net.IPNetwork ipNetwork))
                {
                    options.KnownIPNetworks.Add(ipNetwork);
                }
            }
        });

        return services;
    }
}

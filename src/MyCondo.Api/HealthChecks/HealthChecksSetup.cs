using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MyCondo.Api.HealthChecks;

public static class HealthChecksSetup
{
    public static IServiceCollection AddMyCondoHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IHealthChecksBuilder builder = services.AddHealthChecks();

        string? dbConn = configuration.GetConnectionString("Default")
            ?? configuration["MYCONDO_DB_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(dbConn))
        {
            builder.AddNpgSql(dbConn, name: "postgres", tags: ["ready"]);
        }

        string? redisConn = configuration.GetConnectionString("Redis")
            ?? configuration["MYCONDO_REDIS_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            builder.AddRedis(redisConn, name: "redis", tags: ["ready"]);
        }

        return services;
    }

    public static WebApplication MapMyCondoHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false   // liveness: process is up
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponse
        });

        return app;
    }

    private static Task WriteHealthResponse(HttpContext ctx, HealthReport report) =>
        ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        });
}

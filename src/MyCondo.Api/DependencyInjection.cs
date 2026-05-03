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
                ?? ["http://localhost:5173"];

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
        });

        return services;
    }
}

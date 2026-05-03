using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Domain.Abstractions;
using MyCondo.Infrastructure.Identity;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using MyCondo.Infrastructure.Time;
using StackExchange.Redis;

namespace MyCondo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain abstractions
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidV7IdGenerator>();

        // EF Core SaveChanges interceptors
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        // DbContext
        services.AddDbContext<MyCondoDbContext>((sp, options) =>
        {
            string? connectionString = configuration.GetConnectionString("Default")
                ?? configuration["MYCONDO_DB_CONNECTION_STRING"];

            options.UseNpgsql(connectionString, npg =>
                {
                    npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public");
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<DispatchDomainEventsInterceptor>());

            if (string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Development",
                    StringComparison.Ordinal))
            {
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyCondoDbContext>());

        // Redis (lazy singleton)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string connectionString = configuration.GetConnectionString("Redis")
                ?? configuration["MYCONDO_REDIS_CONNECTION_STRING"]
                ?? "localhost:6379";

            return ConnectionMultiplexer.Connect(connectionString);
        });

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Infrastructure.Persistence.Interceptors;

namespace MyCondo.Infrastructure.Persistence;

/// <summary>
/// Builds a <see cref="TenantScopedUnitOfWork"/> against its own private <see cref="MyCondoDbContext"/>
/// bound to a fixed-tenant <see cref="ITenantContextAccessor"/> — the same technique
/// <c>ArpDevelopmentBootstrapSeeder.BuildDbContext</c> uses, reused here so a real Mediator command
/// handler can do the same thing without depending on Infrastructure/EF Core directly. Never touches
/// the shared, request-scoped <see cref="ITenantContextAccessor"/>/<see cref="TenantContextConnectionInterceptor"/>
/// pair that ambient tenant/platform requests use — that pair stays exactly as security-sensitive and
/// JWT-derived as it already is.
/// </summary>
public sealed class TenantScopedUnitOfWorkFactory(
    IServiceProvider serviceProvider,
    IConfiguration configuration
) : ITenantScopedUnitOfWorkFactory
{
    public ITenantScopedUnitOfWork Create(Guid tenantId)
    {
        string connectionString = configuration.GetConnectionString("Default")
            ?? configuration["MYCONDO_DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        FixedTenantContextAccessor tenantAccessor = new(tenantId);
        TenantContextConnectionInterceptor tenantInterceptor = new(tenantAccessor);

        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>(),
                tenantInterceptor)
            .Options;

        return new TenantScopedUnitOfWork(new MyCondoDbContext(options));
    }

    private sealed class FixedTenantContextAccessor(Guid tenantId) : ITenantContextAccessor
    {
        public Guid? CurrentTenantId => tenantId;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Development-only bootstrap: provisions one default tenant (slug "demo") if none exists, so
/// `POST /api/v1/auth/register` has a real tenant to register against locally. Only registered when
/// the host environment is Development (see Program.cs) — this is not a production provisioning
/// mechanism. Production tenant provisioning is an open question (mycondo-api/docs/kickoff.md §11)
/// and real tenant creation goes through `POST /api/v1/tenants`, which requires the `tenant.manage`
/// permission once the permission catalogue is seeded (see MASTER_BACKLOG.md ID-2).
/// </summary>
public sealed class DevelopmentTenantSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DevelopmentTenantSeeder> logger
) : IHostedService
{
    private const string DefaultSlug = "demo";
    private const string DefaultName = "Demo Tenant";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        bool anyTenantExists = await tenants.AnyAsync(cancellationToken);
        if (anyTenantExists)
        {
            return;
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        Tenant tenant = Tenant.Provision(DefaultName, DefaultSlug, nowUtc);
        tenant.Activate(nowUtc);

        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development seed: provisioned default tenant {TenantId} (slug '{Slug}')",
            tenant.Id, tenant.Slug);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

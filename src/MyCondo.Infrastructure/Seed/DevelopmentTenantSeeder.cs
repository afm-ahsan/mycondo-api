using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Development-only bootstrap: provisions one default tenant (slug "demo") if none exists, so
/// `POST /api/v1/auth/register` has a real tenant to register against locally. Invoked explicitly by
/// <c>DatabaseSeederExtensions.SeedDatabaseAsync</c> only in Development (see that class) — this is not
/// a production provisioning mechanism. Production tenant provisioning is an open question
/// (mycondo-api/docs/kickoff.md §11) and real tenant creation goes through `POST /api/v1/tenants`,
/// which requires the `tenant.manage` permission once the permission catalogue is seeded (see
/// MASTER_BACKLOG.md ID-2).
///
/// A true one-time singleton bootstrap — "any tenant exists" is a permanent, correct guard here (there
/// is no catalogue to drift, unlike <see cref="ArpDevelopmentBootstrapSeeder"/>'s role catalogues).
/// </summary>
public sealed class DevelopmentTenantSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DevelopmentTenantSeeder> logger
)
{
    private const string DefaultSlug = "demo";
    private const string DefaultName = "Demo Tenant";

    public async Task SeedAsync(CancellationToken cancellationToken)
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
            "[DatabaseSeed] Demo tenant: created {TenantId} (slug '{Slug}')",
            tenant.Id, tenant.Slug);
    }
}

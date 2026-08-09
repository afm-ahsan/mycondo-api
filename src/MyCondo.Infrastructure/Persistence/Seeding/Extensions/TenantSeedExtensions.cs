using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Seeding.Extensions;

/// <summary>Idempotent tenant/organization provisioning for seeding. <c>tenancy.tenants</c> carries no
/// RLS policy (it isn't itself tenant-scoped — see mycondo-seed-data-migration-database-audit.md), so
/// this lookup needs no ambient tenant context.</summary>
internal static class TenantSeedExtensions
{
    public static async Task<Tenant> EnsureTenantAsync(
        this ITenantRepository tenants,
        string name,
        string slug,
        IClock clock,
        CancellationToken cancellationToken)
    {
        Tenant? existing = await tenants.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        Tenant tenant = Tenant.Provision(name, slug, nowUtc);
        tenant.Activate(nowUtc);
        tenants.Add(tenant);

        return tenant;
    }
}

using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Common.Services;

/// <summary>Production implementation of <see cref="ITenantLifecycleAccessService"/> — one query
/// (<c>Tenant</c> by id), no caching, matching <see cref="TenantEntitlementService"/>'s baseline (ADR-032
/// Task 10 §45/§86; no Redis).</summary>
public sealed class TenantLifecycleAccessService(ITenantRepository tenants) : ITenantLifecycleAccessService
{
    public async Task<TenantStatus> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        return tenant.Status;
    }
}

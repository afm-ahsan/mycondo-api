namespace MyCondo.Domain.Features.Tenancy;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    /// <summary>Used by platform-scope organization listing (see PlatformOrganizationEndpoints) —
    /// not RLS-filtered, since <c>tenancy.tenants</c> itself has no tenant_id/RLS policy.</summary>
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Tenant tenant);
}

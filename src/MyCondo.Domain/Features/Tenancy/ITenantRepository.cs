using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Tenancy;

public interface ITenantRepository
{
    /// <summary>Platform-scope organization listing — paginated, optionally filtered by a
    /// case-insensitive name/code/slug search term and/or status. Not RLS-filtered (see
    /// <see cref="GetAllAsync"/>'s doc comment).</summary>
    Task<PagedResult<Tenant>> SearchAsync(
        int page, int pageSize, string? search, TenantStatus? status, CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Case-insensitive exact match on the tenant's display name — used as a fallback by
    /// tenant sign-in's organization lookup, since end users identify their organization by name,
    /// not by its internal slug.</summary>
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    /// <summary>Used by platform-scope organization listing (see PlatformOrganizationEndpoints) —
    /// not RLS-filtered, since <c>tenancy.tenants</c> itself has no tenant_id/RLS policy.</summary>
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Tenant tenant);
}

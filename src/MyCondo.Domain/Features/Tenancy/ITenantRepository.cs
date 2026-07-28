namespace MyCondo.Domain.Features.Tenancy;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
    void Add(Tenant tenant);
}

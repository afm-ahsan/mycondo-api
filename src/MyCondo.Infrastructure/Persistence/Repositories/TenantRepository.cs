using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(MyCondoDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken) =>
        db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == new TenantId(id), cancellationToken);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        db.Set<Tenant>().FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        db.Set<Tenant>().FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, name), cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        db.Set<Tenant>().AnyAsync(t => t.Slug == slug, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        db.Set<Tenant>().AnyAsync(cancellationToken);

    public Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<Tenant>().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public void Add(Tenant tenant) => db.Set<Tenant>().Add(tenant);
}

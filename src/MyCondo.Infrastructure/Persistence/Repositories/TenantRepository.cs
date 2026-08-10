using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(MyCondoDbContext db) : ITenantRepository
{
    public async Task<PagedResult<Tenant>> SearchAsync(
        int page, int pageSize, string? search, TenantStatus? status, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = db.Set<Tenant>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, term) || EF.Functions.ILike(t.Slug, term)
                || (t.Code != null && EF.Functions.ILike(t.Code, term)));
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        query = query.OrderByDescending(t => t.CreatedAtUtc);

        long total = await query.LongCountAsync(cancellationToken);
        List<Tenant> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Tenant>(items, page, pageSize, total);
    }

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

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Set<Tenant>().AnyAsync(t => t.Code == code, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        db.Set<Tenant>().AnyAsync(cancellationToken);

    public Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<Tenant>().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public void Add(Tenant tenant) => db.Set<Tenant>().Add(tenant);
}

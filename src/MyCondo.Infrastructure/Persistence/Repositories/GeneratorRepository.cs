using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorRepository(MyCondoDbContext db) : IGeneratorRepository
{
    public Task<Generator?> GetByIdAsync(GeneratorId id, CancellationToken cancellationToken) =>
        db.Set<Generator>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<PagedResult<Generator>> SearchAsync(
        Guid tenantId, BuildingId? buildingId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Generator> query = db.Set<Generator>()
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(g => g.BuildingId == buildingId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Generator> items = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Generator>(items, page, pageSize, total);
    }

    public async Task LockForSessionStartCheckAsync(GeneratorId id, CancellationToken cancellationToken) =>
        await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM operations.generators WHERE id = {id.Value} FOR UPDATE")
            .ToListAsync(cancellationToken);

    public void Add(Generator generator) => db.Set<Generator>().Add(generator);
}

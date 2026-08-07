using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorServiceRecordRepository(MyCondoDbContext db) : IGeneratorServiceRecordRepository
{
    public Task<GeneratorServiceRecord?> GetByIdAsync(GeneratorServiceRecordId id, CancellationToken cancellationToken) =>
        db.Set<GeneratorServiceRecord>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<GeneratorServiceRecord>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorServiceRecord> query = db.Set<GeneratorServiceRecord>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (generatorId is not null)
        {
            query = query.Where(x => x.GeneratorId == generatorId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GeneratorServiceRecord> items = await query
            .OrderByDescending(x => x.PerformedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GeneratorServiceRecord>(items, page, pageSize, total);
    }

    public void Add(GeneratorServiceRecord record) => db.Set<GeneratorServiceRecord>().Add(record);
}

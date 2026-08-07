using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorBreakdownRecordRepository(MyCondoDbContext db) : IGeneratorBreakdownRecordRepository
{
    public Task<GeneratorBreakdownRecord?> GetByIdAsync(GeneratorBreakdownRecordId id, CancellationToken cancellationToken) =>
        db.Set<GeneratorBreakdownRecord>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<GeneratorBreakdownRecord>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorBreakdownRecord> query = db.Set<GeneratorBreakdownRecord>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (generatorId is not null)
        {
            query = query.Where(x => x.GeneratorId == generatorId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GeneratorBreakdownRecord> items = await query
            .OrderByDescending(x => x.ReportedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GeneratorBreakdownRecord>(items, page, pageSize, total);
    }

    public void Add(GeneratorBreakdownRecord record) => db.Set<GeneratorBreakdownRecord>().Add(record);
}

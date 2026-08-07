using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorSessions;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorSessionRepository(MyCondoDbContext db) : IGeneratorSessionRepository
{
    public Task<GeneratorSession?> GetByIdAsync(GeneratorSessionId id, CancellationToken cancellationToken) =>
        db.Set<GeneratorSession>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<GeneratorSession?> GetOpenForGeneratorAsync(
        Guid tenantId, GeneratorId generatorId, CancellationToken cancellationToken) =>
        db.Set<GeneratorSession>()
            .Where(s => s.TenantId == tenantId && s.GeneratorId == generatorId && s.Status == GeneratorSessionStatus.Open)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<GeneratorSession>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, GeneratorSessionStatus? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<GeneratorSession> query = db.Set<GeneratorSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (generatorId is not null)
        {
            query = query.Where(s => s.GeneratorId == generatorId);
        }

        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GeneratorSession> items = await query
            .OrderByDescending(s => s.StartAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GeneratorSession>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<GeneratorSession>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, GeneratorId? generatorId, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorSession> query = db.Set<GeneratorSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == GeneratorSessionStatus.Closed
                && s.StartAtUtc >= fromUtc && s.StartAtUtc <= toUtc);

        if (generatorId is not null)
        {
            query = query.Where(s => s.GeneratorId == generatorId);
        }

        return await query.OrderBy(s => s.StartAtUtc).ToListAsync(cancellationToken);
    }

    public void Add(GeneratorSession session) => db.Set<GeneratorSession>().Add(session);
}

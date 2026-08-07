using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorMaintenanceScheduleRepository(MyCondoDbContext db) : IGeneratorMaintenanceScheduleRepository
{
    public Task<GeneratorMaintenanceSchedule?> GetByIdAsync(GeneratorMaintenanceScheduleId id, CancellationToken cancellationToken) =>
        db.Set<GeneratorMaintenanceSchedule>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<GeneratorMaintenanceSchedule>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorMaintenanceSchedule> query = db.Set<GeneratorMaintenanceSchedule>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (generatorId is not null)
        {
            query = query.Where(x => x.GeneratorId == generatorId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GeneratorMaintenanceSchedule> items = await query
            .OrderBy(x => x.NextDueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GeneratorMaintenanceSchedule>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<GeneratorMaintenanceSchedule>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<GeneratorMaintenanceSchedule>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .ToListAsync(cancellationToken);

    public void Add(GeneratorMaintenanceSchedule schedule) => db.Set<GeneratorMaintenanceSchedule>().Add(schedule);
}

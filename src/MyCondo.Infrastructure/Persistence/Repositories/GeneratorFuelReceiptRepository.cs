using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GeneratorFuelReceiptRepository(MyCondoDbContext db) : IGeneratorFuelReceiptRepository
{
    public Task<GeneratorFuelReceipt?> GetByIdAsync(GeneratorFuelReceiptId id, CancellationToken cancellationToken) =>
        db.Set<GeneratorFuelReceipt>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<GeneratorFuelReceipt>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorFuelReceipt> query = db.Set<GeneratorFuelReceipt>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (generatorId is not null)
        {
            query = query.Where(x => x.GeneratorId == generatorId);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<GeneratorFuelReceipt> items = await query
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GeneratorFuelReceipt>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<GeneratorFuelReceipt>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, GeneratorId? generatorId, CancellationToken cancellationToken)
    {
        IQueryable<GeneratorFuelReceipt> query = db.Set<GeneratorFuelReceipt>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ReceivedAtUtc >= fromUtc && x.ReceivedAtUtc <= toUtc);

        if (generatorId is not null)
        {
            query = query.Where(x => x.GeneratorId == generatorId);
        }

        return await query.OrderBy(x => x.ReceivedAtUtc).ToListAsync(cancellationToken);
    }

    public void Add(GeneratorFuelReceipt receipt) => db.Set<GeneratorFuelReceipt>().Add(receipt);
}

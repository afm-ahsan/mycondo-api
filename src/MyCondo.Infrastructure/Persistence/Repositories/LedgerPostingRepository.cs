using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class LedgerPostingRepository(MyCondoDbContext db) : ILedgerPostingRepository
{
    public void Add(LedgerPosting posting) => db.Set<LedgerPosting>().Add(posting);

    public Task<bool> ExistsAsync(Guid tenantId, string referenceType, Guid sourceId, CancellationToken cancellationToken) =>
        db.Set<LedgerPosting>().AnyAsync(
            x => x.TenantId == tenantId && x.ReferenceType == referenceType && x.ReferenceId == sourceId,
            cancellationToken);
}

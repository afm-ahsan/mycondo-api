using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Payments.Idempotency;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyKeyRepository(MyCondoDbContext db) : IIdempotencyKeyRepository
{
    public Task<IdempotencyKey?> FindAsync(Guid tenantId, string key, string requestPath, CancellationToken cancellationToken) =>
        db.Set<IdempotencyKey>().FirstOrDefaultAsync(
            k => k.TenantId == tenantId && k.Key == key && k.RequestPath == requestPath, cancellationToken);

    public void Add(IdempotencyKey idempotencyKey) => db.Set<IdempotencyKey>().Add(idempotencyKey);
}

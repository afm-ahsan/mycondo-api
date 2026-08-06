namespace MyCondo.Domain.Features.Payments.Idempotency;

public interface IIdempotencyKeyRepository
{
    Task<IdempotencyKey?> FindAsync(
        Guid tenantId, string key, string requestPath, CancellationToken cancellationToken);

    void Add(IdempotencyKey idempotencyKey);
}

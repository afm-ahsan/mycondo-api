using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Idempotency.Exceptions;

namespace MyCondo.Domain.Features.Payments.Idempotency;

/// <summary>
/// A recorded response to a previously-executed financial mutation, keyed by the caller-supplied
/// <c>X-Idempotency-Key</c> header plus request path. A replay with the same key and an identical
/// request body returns the stored response without re-executing the mutation; a replay with the same
/// key but a different body is a client bug and is rejected via <see cref="IdempotencyKeyConflictException"/>.
/// Written once, per-request, after the mutation succeeds — never updated.
/// </summary>
public sealed class IdempotencyKey : AggregateRoot<IdempotencyKeyId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Key { get; private set; } = null!;
    public string RequestPath { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public int ResponseStatusCode { get; private set; }
    public string ResponseBody { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private IdempotencyKey() { }

    private IdempotencyKey(
        IdempotencyKeyId id,
        Guid tenantId,
        string key,
        string requestPath,
        string requestHash,
        int responseStatusCode,
        string responseBody,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Key = key;
        RequestPath = requestPath;
        RequestHash = requestHash;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        CreatedAtUtc = nowUtc;
    }

    public static IdempotencyKey Record(
        Guid tenantId,
        string key,
        string requestPath,
        string requestHash,
        int responseStatusCode,
        string responseBody,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new IdempotencyKey(
            IdempotencyKeyId.New(), tenantId, key.Trim(), requestPath.Trim(), requestHash, responseStatusCode,
            responseBody, nowUtc);
    }

    /// <summary>
    /// Confirms a replayed request matches the one originally recorded under this key. Callers should
    /// return <see cref="ResponseStatusCode"/>/<see cref="ResponseBody"/> as-is on success rather than
    /// re-running the mutation.
    /// </summary>
    public void EnsureMatches(string requestHash)
    {
        if (!string.Equals(RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new IdempotencyKeyConflictException(Key);
        }
    }
}

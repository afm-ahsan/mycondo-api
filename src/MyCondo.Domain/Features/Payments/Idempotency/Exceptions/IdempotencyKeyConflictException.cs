using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Payments.Idempotency.Exceptions;

/// <summary>
/// Thrown when an <c>X-Idempotency-Key</c> is replayed against a different request body than the
/// one it was first recorded against — reusing a key for a materially different mutation is a client
/// bug, never a legitimate retry.
/// </summary>
public sealed class IdempotencyKeyConflictException(string key)
    : DomainException($"Idempotency key '{key}' was already used for a different request.")
{
    public string Key { get; } = key;
}

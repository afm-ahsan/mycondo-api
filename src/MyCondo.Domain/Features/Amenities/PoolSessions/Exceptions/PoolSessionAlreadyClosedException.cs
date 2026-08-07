using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Amenities.PoolSessions.Exceptions;

public sealed class PoolSessionAlreadyClosedException(PoolSessionId poolSessionId)
    : DomainException($"Pool session {poolSessionId} is already checked out.")
{
    public PoolSessionId PoolSessionId { get; } = poolSessionId;
}

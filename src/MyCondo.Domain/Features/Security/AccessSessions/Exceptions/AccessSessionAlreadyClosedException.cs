using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Security.AccessSessions.Exceptions;

public sealed class AccessSessionAlreadyClosedException(AccessSessionId accessSessionId)
    : DomainException($"Access session {accessSessionId} is already checked out.")
{
    public AccessSessionId AccessSessionId { get; } = accessSessionId;
}

using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;

public sealed class GeneratorSessionAlreadyClosedException(GeneratorSessionId id)
    : DomainException($"Generator session {id} is already stopped.")
{
    public GeneratorSessionId GeneratorSessionId { get; } = id;
}

using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Operations.Generators.Exceptions;

public sealed class GeneratorInactiveException(GeneratorId id)
    : DomainException($"Generator {id} is inactive and cannot start a new session.")
{
    public GeneratorId GeneratorId { get; } = id;
}

using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;

/// <summary>Enforces "only one open session per generator" (register-digitization spec §5.13). Raised
/// by <c>StartGeneratorSessionCommandHandler</c> after locking the generator row — see
/// <c>IGeneratorRepository.LockForSessionStartCheckAsync</c>.</summary>
public sealed class GeneratorAlreadyHasOpenSessionException(GeneratorId generatorId)
    : DomainException($"Generator {generatorId} already has an open session. Stop it before starting a new one.")
{
    public GeneratorId GeneratorId { get; } = generatorId;
}

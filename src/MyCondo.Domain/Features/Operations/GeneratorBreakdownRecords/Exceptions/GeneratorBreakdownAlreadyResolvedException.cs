using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords.Exceptions;

public sealed class GeneratorBreakdownAlreadyResolvedException(GeneratorBreakdownRecordId id)
    : DomainException($"Generator breakdown {id} is already resolved.")
{
    public GeneratorBreakdownRecordId GeneratorBreakdownRecordId { get; } = id;
}

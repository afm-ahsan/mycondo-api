using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;

public interface IGeneratorBreakdownRecordRepository
{
    Task<GeneratorBreakdownRecord?> GetByIdAsync(GeneratorBreakdownRecordId id, CancellationToken cancellationToken);

    Task<PagedResult<GeneratorBreakdownRecord>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken);

    void Add(GeneratorBreakdownRecord record);
}

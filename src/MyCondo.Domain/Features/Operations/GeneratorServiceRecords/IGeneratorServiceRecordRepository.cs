using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

public interface IGeneratorServiceRecordRepository
{
    Task<GeneratorServiceRecord?> GetByIdAsync(GeneratorServiceRecordId id, CancellationToken cancellationToken);

    Task<PagedResult<GeneratorServiceRecord>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken);

    void Add(GeneratorServiceRecord record);
}

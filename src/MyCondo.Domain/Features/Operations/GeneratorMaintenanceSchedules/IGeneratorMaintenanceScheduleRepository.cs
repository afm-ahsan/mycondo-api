using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;

public interface IGeneratorMaintenanceScheduleRepository
{
    Task<GeneratorMaintenanceSchedule?> GetByIdAsync(GeneratorMaintenanceScheduleId id, CancellationToken cancellationToken);

    Task<PagedResult<GeneratorMaintenanceSchedule>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Unpaged, for the "maintenance due" report — active schedules across every generator.</summary>
    Task<IReadOnlyList<GeneratorMaintenanceSchedule>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(GeneratorMaintenanceSchedule schedule);
}

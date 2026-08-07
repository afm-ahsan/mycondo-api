using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorSessions;

public interface IGeneratorSessionRepository
{
    Task<GeneratorSession?> GetByIdAsync(GeneratorSessionId id, CancellationToken cancellationToken);

    Task<GeneratorSession?> GetOpenForGeneratorAsync(
        Guid tenantId, GeneratorId generatorId, CancellationToken cancellationToken);

    Task<PagedResult<GeneratorSession>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, GeneratorSessionStatus? status, int page, int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Closed sessions that started within the period, optionally scoped to one generator —
    /// feeds the runtime/fuel-usage/cost-per-hour report.</summary>
    Task<IReadOnlyList<GeneratorSession>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, GeneratorId? generatorId, CancellationToken cancellationToken);

    void Add(GeneratorSession session);
}

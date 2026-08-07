using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

public interface IMonthlyCylinderReconciliationRepository
{
    Task<MonthlyCylinderReconciliation?> GetByIdAsync(MonthlyCylinderReconciliationId id, CancellationToken cancellationToken);

    Task<PagedResult<MonthlyCylinderReconciliation>> SearchAsync(
        Guid tenantId, string? cylinderType, int page, int pageSize, CancellationToken cancellationToken);

    void Add(MonthlyCylinderReconciliation reconciliation);
}

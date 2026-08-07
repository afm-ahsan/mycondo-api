using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Domain.Features.Operations.CylinderPurchases;

public interface ICylinderPurchaseRepository
{
    Task<CylinderPurchase?> GetByIdAsync(CylinderPurchaseId id, CancellationToken cancellationToken);

    Task<PagedResult<CylinderPurchase>> SearchAsync(
        Guid tenantId, GasCylinderSupplierId? supplierId, CylinderPurchaseApprovalStatus? approvalStatus, int page,
        int pageSize, CancellationToken cancellationToken);

    /// <summary>Unpaged, for consumption/supplier-comparison reports over a date range.</summary>
    Task<IReadOnlyList<CylinderPurchase>> GetForPeriodAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    void Add(CylinderPurchase purchase);
}

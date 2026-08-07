using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Operations.CylinderStockMovements;

public interface ICylinderStockMovementRepository
{
    Task<CylinderStockMovement?> GetByIdAsync(CylinderStockMovementId id, CancellationToken cancellationToken);

    Task<PagedResult<CylinderStockMovement>> SearchAsync(
        Guid tenantId, string? cylinderType, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Signed-quantity sum of every movement for a cylinder type — current stock.</summary>
    Task<int> GetCurrentStockAsync(Guid tenantId, string cylinderType, CancellationToken cancellationToken);

    /// <summary>Distinct cylinder types with any movement recorded, for stock/report listings.</summary>
    Task<IReadOnlyList<string>> ListDistinctCylinderTypesAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Unpaged, for monthly reconciliation and consumption reports.</summary>
    Task<IReadOnlyList<CylinderStockMovement>> GetForPeriodAsync(
        Guid tenantId, string cylinderType, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    void Add(CylinderStockMovement movement);
}

using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

public interface IGasCylinderSupplierRepository
{
    Task<GasCylinderSupplier?> GetByIdAsync(GasCylinderSupplierId id, CancellationToken cancellationToken);

    Task<PagedResult<GasCylinderSupplier>> SearchAsync(
        Guid tenantId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Unpaged, for the supplier-comparison report.</summary>
    Task<IReadOnlyList<GasCylinderSupplier>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(GasCylinderSupplier supplier);
}

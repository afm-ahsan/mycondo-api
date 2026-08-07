using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;

public interface IGeneratorFuelReceiptRepository
{
    Task<GeneratorFuelReceipt?> GetByIdAsync(GeneratorFuelReceiptId id, CancellationToken cancellationToken);

    Task<PagedResult<GeneratorFuelReceipt>> SearchAsync(
        Guid tenantId, GeneratorId? generatorId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Unpaged, for reconciliation/report aggregation over a date range, optionally scoped to
    /// one generator — mirrors <c>IBookingRepository.GetForPeriodAsync</c>'s report-query shape.</summary>
    Task<IReadOnlyList<GeneratorFuelReceipt>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, GeneratorId? generatorId, CancellationToken cancellationToken);

    void Add(GeneratorFuelReceipt receipt);
}

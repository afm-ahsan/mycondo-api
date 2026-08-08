using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken);

    Task<PagedResult<Payment>> SearchAsync(
        Guid tenantId,
        FlatId? flatId,
        PaymentStatus? status,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Sum of Posted (non-reversed) payment amounts with BusinessDate in [fromDate, toDate],
    /// optionally scoped to a building via the Flat a payment was recorded against — Payment has no
    /// BuildingId of its own, so this is the simplest authoritative relationship, not payment allocations
    /// (a single payment's allocations can span multiple invoices).</summary>
    Task<decimal> GetTotalCollectedAsync(
        Guid tenantId, BuildingId? buildingId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    void Add(Payment payment);
}

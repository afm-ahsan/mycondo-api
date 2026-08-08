using MyCondo.Domain.Common;
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

    void Add(Payment payment);
}

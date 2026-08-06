using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken);

    Task<PagedResult<Payment>> SearchForFlatAsync(
        Guid tenantId, FlatId flatId, int page, int pageSize, CancellationToken cancellationToken);

    void Add(Payment payment);
}

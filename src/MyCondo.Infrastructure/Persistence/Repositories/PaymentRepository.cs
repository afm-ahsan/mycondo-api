using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(MyCondoDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken) =>
        db.Set<Payment>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<Payment>> SearchForFlatAsync(
        Guid tenantId, FlatId flatId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = db.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.FlatId == flatId);

        long total = await query.LongCountAsync(cancellationToken);

        List<Payment> items = await query
            .OrderByDescending(p => p.BusinessDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, page, pageSize, total);
    }

    public void Add(Payment payment) => db.Set<Payment>().Add(payment);
}

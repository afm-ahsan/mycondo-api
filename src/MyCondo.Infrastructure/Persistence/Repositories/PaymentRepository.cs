using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(MyCondoDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken) =>
        db.Set<Payment>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<Payment>> SearchAsync(
        Guid tenantId,
        FlatId? flatId,
        PaymentStatus? status,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = db.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (flatId is not null)
        {
            query = query.Where(p => p.FlatId == flatId);
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        if (paymentMethod is not null)
        {
            query = query.Where(p => p.PaymentMethod == paymentMethod);
        }

        if (fromDate is not null)
        {
            query = query.Where(p => p.BusinessDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(p => p.BusinessDate <= toDate);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Payment> items = await query
            .OrderByDescending(p => p.BusinessDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, page, pageSize, total);
    }

    public async Task<decimal> GetTotalCollectedAsync(
        Guid tenantId, BuildingId? buildingId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = db.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == PaymentStatus.Posted
                && p.BusinessDate >= fromDate && p.BusinessDate <= toDate);

        if (buildingId is not null)
        {
            BuildingId requiredBuildingId = buildingId.Value;
            query = query.Where(p => db.Set<Flat>().Any(f => f.Id == p.FlatId && f.BuildingId == requiredBuildingId));
        }

        return await query.SumAsync(p => p.Amount, cancellationToken);
    }

    public void Add(Payment payment) => db.Set<Payment>().Add(payment);
}

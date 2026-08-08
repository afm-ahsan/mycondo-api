using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(MyCondoDbContext db) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(BookingId id, CancellationToken cancellationToken) =>
        db.Set<Booking>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<PagedResult<Booking>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, FlatId? flatId, BookingStatus? status, BuildingId? buildingId,
        string? eventType, BookingPaymentStatus? paymentStatus, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Booking> query = db.Set<Booking>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId);

        if (facilityId is not null)
        {
            query = query.Where(b => b.FacilityId == facilityId);
        }

        if (flatId is not null)
        {
            query = query.Where(b => b.FlatId == flatId);
        }

        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }

        if (buildingId is not null)
        {
            query = query.Where(b => b.BuildingId == buildingId);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(b => EF.Functions.ILike(b.EventType, $"%{eventType}%"));
        }

        if (paymentStatus is not null)
        {
            query = paymentStatus switch
            {
                BookingPaymentStatus.NotRequired => query.Where(b => !b.PaymentRequired),
                BookingPaymentStatus.Paid => query.Where(b =>
                    b.PaymentRequired && (b.InvoiceId != null || b.DepositCollectionPostingId != null)),
                BookingPaymentStatus.AwaitingPayment => query.Where(b =>
                    b.PaymentRequired && b.InvoiceId == null && b.DepositCollectionPostingId == null),
                _ => query,
            };
        }

        if (fromDate is not null)
        {
            query = query.Where(b => b.StartAtUtc >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(b => b.StartAtUtc < toDate);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Booking> items = await query
            .OrderByDescending(b => b.StartAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Booking>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Booking>> GetUpcomingAsync(
        Guid tenantId, DateTimeOffset fromUtc, CancellationToken cancellationToken) =>
        await db.Set<Booking>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StartAtUtc >= fromUtc
                && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected && b.Status != BookingStatus.NoShow)
            .OrderBy(b => b.StartAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOverlappingBookingAsync(
        Guid tenantId, FacilityId facilityId, DateTimeOffset effectiveStartUtc, DateTimeOffset effectiveEndUtc,
        CancellationToken cancellationToken) =>
        db.Set<Booking>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.FacilityId == facilityId
                && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected && b.Status != BookingStatus.NoShow)
            .AnyAsync(b =>
                effectiveStartUtc < b.EndAtUtc.AddMinutes(b.CleanupBufferMinutes)
                && effectiveEndUtc > b.StartAtUtc.AddMinutes(-b.SetupBufferMinutes),
                cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, FacilityId? facilityId, CancellationToken cancellationToken)
    {
        IQueryable<Booking> query = db.Set<Booking>()
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StartAtUtc >= fromUtc && b.StartAtUtc < toUtc);

        if (facilityId is not null)
        {
            query = query.Where(b => b.FacilityId == facilityId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public void Add(Booking booking) => db.Set<Booking>().Add(booking);
}

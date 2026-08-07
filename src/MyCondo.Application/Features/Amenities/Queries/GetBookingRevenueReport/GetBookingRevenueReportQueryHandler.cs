using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingRevenueReport;

public sealed class GetBookingRevenueReportQueryHandler(
    IBookingRepository bookings,
    IFacilityRepository facilities,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBookingRevenueReportQuery, IReadOnlyList<BookingRevenueReportLineDto>>
{
    public async ValueTask<IReadOnlyList<BookingRevenueReportLineDto>> Handle(
        GetBookingRevenueReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateTimeOffset fromUtc = new(query.FromDate, TimeOnly.MinValue, TimeSpan.Zero);
        DateTimeOffset toUtc = new(query.ToDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        IReadOnlyList<Booking> periodBookings = await bookings.GetForPeriodAsync(tenantId, fromUtc, toUtc, null, cancellationToken);

        List<BookingRevenueReportLineDto> lines = [];
        foreach (IGrouping<FacilityId, Booking> group in periodBookings.GroupBy(b => b.FacilityId))
        {
            Facility? facility = await facilities.GetByIdAsync(group.Key, cancellationToken);
            decimal totalCharges = group.Where(b => b.InvoiceId is not null).Sum(b => b.BookingChargeAmount);
            decimal totalForfeited = group.Sum(b => b.DepositDeductedAmount ?? 0m);
            int billableCount = group.Count(b => b.InvoiceId is not null);

            lines.Add(new BookingRevenueReportLineDto(
                group.Key.Value, facility?.Name ?? "(unknown)", totalCharges, totalForfeited, billableCount));
        }

        return lines;
    }
}

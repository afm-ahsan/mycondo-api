using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilityUtilizationReport;

public sealed class GetFacilityUtilizationReportQueryHandler(
    IBookingRepository bookings,
    IFacilityRepository facilities,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFacilityUtilizationReportQuery, IReadOnlyList<FacilityUtilizationReportLineDto>>
{
    public async ValueTask<IReadOnlyList<FacilityUtilizationReportLineDto>> Handle(
        GetFacilityUtilizationReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId? facilityId = query.FacilityId is Guid rawFacilityId ? new FacilityId(rawFacilityId) : null;
        DateTimeOffset fromUtc = new(query.FromDate, TimeOnly.MinValue, TimeSpan.Zero);
        DateTimeOffset toUtc = new(query.ToDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        IReadOnlyList<Booking> periodBookings = await bookings.GetForPeriodAsync(tenantId, fromUtc, toUtc, facilityId, cancellationToken);

        List<FacilityUtilizationReportLineDto> lines = [];
        foreach (IGrouping<FacilityId, Booking> group in periodBookings.GroupBy(b => b.FacilityId))
        {
            Facility? facility = await facilities.GetByIdAsync(group.Key, cancellationToken);
            int completed = group.Count(b => b.Status is BookingStatus.Completed or BookingStatus.ClosedAfterInspection);
            int cancelled = group.Count(b => b.Status == BookingStatus.Cancelled);
            int noShow = group.Count(b => b.Status == BookingStatus.NoShow);

            lines.Add(new FacilityUtilizationReportLineDto(
                group.Key.Value, facility?.Name ?? "(unknown)", group.Count(), completed, cancelled, noShow));
        }

        return lines;
    }
}

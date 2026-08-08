using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookings;

public sealed class GetBookingsQueryHandler(
    IBookingRepository bookings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBookingsQuery, PagedResult<BookingDto>>
{
    public async ValueTask<PagedResult<BookingDto>> Handle(GetBookingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId? facilityId = query.FacilityId is Guid rawFacilityId ? new FacilityId(rawFacilityId) : null;
        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;
        BookingStatus? status = query.Status is null ? null : Enum.Parse<BookingStatus>(query.Status);
        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        BookingPaymentStatus? paymentStatus = query.PaymentStatus is null
            ? null
            : Enum.Parse<BookingPaymentStatus>(query.PaymentStatus);

        PagedResult<Booking> result = await bookings.SearchAsync(
            tenantId, facilityId, flatId, status, buildingId, query.EventType, paymentStatus, query.FromDate,
            query.ToDate, query.Page, query.PageSize, cancellationToken);

        List<BookingDto> items = result.Items.Select(b => b.ToDto()).ToList();

        return new PagedResult<BookingDto>(items, result.Page, result.PageSize, result.Total);
    }
}

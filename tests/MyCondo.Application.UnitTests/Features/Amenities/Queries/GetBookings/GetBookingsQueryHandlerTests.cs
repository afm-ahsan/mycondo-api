using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Queries.GetBookings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Queries.GetBookings;

/// <summary>
/// Handler-level tests proving the query's Guid?/string? wire values are translated into the correct
/// typed repository arguments (FacilityId/FlatId/BuildingId/BookingStatus/BookingPaymentStatus) and
/// passed through unchanged — the actual SQL-level filtering correctness is proven separately by
/// BookingSearchFilterTests (MyCondo.MultiTenancyTests, real Postgres).
/// </summary>
public class GetBookingsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetBookingsQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _bookings.SearchAsync(
                Arg.Any<Guid>(), Arg.Any<FacilityId?>(), Arg.Any<FlatId?>(), Arg.Any<BookingStatus?>(),
                Arg.Any<BuildingId?>(), Arg.Any<string?>(), Arg.Any<BookingPaymentStatus?>(),
                Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Booking>([], 1, 20, 0));
    }

    private GetBookingsQueryHandler CreateHandler() => new(_bookings, _currentUser);

    [Fact]
    public async Task Parses_Every_Filter_And_Passes_Typed_Values_To_The_Repository()
    {
        Guid facilityId = Guid.NewGuid();
        Guid flatId = Guid.NewGuid();
        Guid buildingId = Guid.NewGuid();
        DateTimeOffset fromDate = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset toDate = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        GetBookingsQuery query = new(
            facilityId, flatId, "Confirmed", buildingId, "wedding", "AwaitingPayment", fromDate, toDate, 2, 15);

        await CreateHandler().Handle(query, CancellationToken.None);

        await _bookings.Received(1).SearchAsync(
            TenantId,
            Arg.Is<FacilityId?>(f => f == new FacilityId(facilityId)),
            Arg.Is<FlatId?>(f => f == new FlatId(flatId)),
            BookingStatus.Confirmed,
            Arg.Is<BuildingId?>(b => b == new BuildingId(buildingId)),
            "wedding",
            BookingPaymentStatus.AwaitingPayment,
            fromDate,
            toDate,
            2,
            15,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_Every_Optional_Filter_Null_When_Not_Supplied()
    {
        GetBookingsQuery query = new(null, null, null, null, null, null, null, null, 1, 20);

        await CreateHandler().Handle(query, CancellationToken.None);

        await _bookings.Received(1).SearchAsync(
            TenantId, null, null, null, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>());
    }
}

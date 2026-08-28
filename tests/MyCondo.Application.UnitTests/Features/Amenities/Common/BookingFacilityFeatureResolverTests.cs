using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Common;

/// <summary>ADR-033 Task 09A §14/§29/§45 — <see cref="BookingFacilityFeatureResolver{TRequest}"/> unit
/// tests, including the mandatory proof that the same shared request type resolves differently for
/// Community Hall vs. Swimming Pool bookings (§29/§37).</summary>
public class BookingFacilityFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IFacilityRepository _facilities = Substitute.For<IFacilityRepository>();

    private sealed record Request(Guid BookingId) : IHasBookingId;

    private BookingFacilityFeatureResolver<Request> CreateSut() => new(_bookings, _facilities);

    [Fact]
    public async Task Same_Request_Type_Resolves_Community_Hall_And_Swimming_Pool_Differently_By_Record()
    {
        Guid communityHallBookingId = Guid.NewGuid();
        Guid communityHallFacilityId = Guid.NewGuid();
        Guid poolBookingId = Guid.NewGuid();
        Guid poolFacilityId = Guid.NewGuid();

        _bookings.GetFacilityIdAsync(TenantId, new BookingId(communityHallBookingId), Arg.Any<CancellationToken>())
            .Returns(new FacilityId(communityHallFacilityId));
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(communityHallFacilityId), Arg.Any<CancellationToken>())
            .Returns(FacilityType.CommunityHall);

        _bookings.GetFacilityIdAsync(TenantId, new BookingId(poolBookingId), Arg.Any<CancellationToken>())
            .Returns(new FacilityId(poolFacilityId));
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(poolFacilityId), Arg.Any<CancellationToken>())
            .Returns(FacilityType.SwimmingPool);

        BookingFacilityFeatureResolver<Request> sut = CreateSut();

        string communityHallKey = await sut.ResolveFeatureKeyAsync(new Request(communityHallBookingId), TenantId, CancellationToken.None);
        string poolKey = await sut.ResolveFeatureKeyAsync(new Request(poolBookingId), TenantId, CancellationToken.None);

        communityHallKey.Should().Be("facilities.community_hall");
        poolKey.Should().Be("facilities.swimming_pool");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Booking_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid bookingId = Guid.NewGuid();
        _bookings.GetFacilityIdAsync(TenantId, new BookingId(bookingId), Arg.Any<CancellationToken>())
            .Returns((FacilityId?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(bookingId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Booking_Exists_But_Its_Facility_Cannot_Be_Resolved()
    {
        Guid bookingId = Guid.NewGuid();
        Guid facilityId = Guid.NewGuid();
        _bookings.GetFacilityIdAsync(TenantId, new BookingId(bookingId), Arg.Any<CancellationToken>())
            .Returns(new FacilityId(facilityId));
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns((FacilityType?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(bookingId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

using AwesomeAssertions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real Postgres proofs for the UX-4 `GetBookingsQuery`/`BookingRepository.SearchAsync` filter batch
/// (BuildingId, EventType, PaymentStatus, FromDate/ToDate) — added because a mocked repository test
/// only proves the handler passes parameters through, not that the actual EF LINQ-to-SQL WHERE clause
/// filters correctly. Same pattern as <see cref="AmenitiesCrossTenantIsolationTests"/>. Requires a
/// Docker daemon. Written and reviewed for correctness but NOT executed in the environment they were
/// authored in — run wherever Docker is actually available before trusting them.
/// </summary>
public class BookingSearchFilterTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public BookingSearchFilterTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Booking MakeBooking(
        Guid tenantId, FacilityId facilityId, BuildingId buildingId, FlatId flatId, string eventType,
        DateTimeOffset startAtUtc, decimal bookingChargeAmount = 0m, decimal depositAmount = 0m) =>
        Booking.Request(
            tenantId, facilityId, buildingId, flatId, eventType, startAtUtc, startAtUtc.AddHours(2), 0, 0, 10,
            false, bookingChargeAmount, depositAmount, 24, 0m, null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task FromDate_Only_Returns_Bookings_Starting_On_Or_After_The_Boundary()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset boundary = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Before boundary", boundary.AddDays(-1)));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "On boundary", boundary));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "After boundary", boundary.AddDays(1)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, null, null, null, boundary, null, 1, 20, CancellationToken.None);

        result.Items.Select(b => b.EventType).Should().BeEquivalentTo(["On boundary", "After boundary"]);
    }

    [Fact]
    public async Task ToDate_Only_Returns_Bookings_Starting_Strictly_Before_The_Boundary()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset boundary = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Before boundary", boundary.AddDays(-1)));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "On boundary", boundary));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "After boundary", boundary.AddDays(1)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, null, null, null, null, boundary, 1, 20, CancellationToken.None);

        result.Items.Select(b => b.EventType).Should().BeEquivalentTo(["Before boundary"]);
    }

    [Fact]
    public async Task Date_Range_Returns_Only_Bookings_Starting_Within_The_Half_Open_Interval()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset monthStart = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset monthEnd = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Previous month", monthStart.AddDays(-1)));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "First day", monthStart));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Mid month", monthStart.AddDays(15)));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Next month", monthEnd));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, null, null, null, monthStart, monthEnd, 1, 20, CancellationToken.None);

        result.Items.Select(b => b.EventType).Should().BeEquivalentTo(["First day", "Mid month"]);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task Pagination_Within_A_Date_Range_Reports_The_Filtered_Total_Not_The_Unfiltered_One()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset monthStart = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset monthEnd = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        for (int i = 0; i < 5; i++)
        {
            db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, $"In range {i}", monthStart.AddDays(i)));
        }
        // Outside the requested range — must not count toward total or appear in any page.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Outside range", monthEnd.AddDays(5)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> page1 = await repository.SearchAsync(
            tenantId, null, null, null, null, null, null, monthStart, monthEnd, 1, 2, CancellationToken.None);
        PagedResult<Booking> page2 = await repository.SearchAsync(
            tenantId, null, null, null, null, null, null, monthStart, monthEnd, 2, 2, CancellationToken.None);

        page1.Total.Should().Be(5);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.Items.Select(b => b.EventType).Should().NotIntersectWith(page2.Items.Select(b => b.EventType));
    }

    [Fact]
    public async Task BuildingId_Filter_Returns_Only_Bookings_For_That_Building()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(10);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingA, flatId, "Building A event", start));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingB, flatId, "Building B event", start.AddHours(5)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, buildingA, null, null, null, null, 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(b => b.EventType == "Building A event");
    }

    [Fact]
    public async Task EventType_Filter_Is_A_Case_Insensitive_Contains_Match()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(10);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Birthday Party", start));
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Anniversary Dinner", start.AddHours(5)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, null, "birthday", null, null, null, 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(b => b.EventType == "Birthday Party");
    }

    [Theory]
    [InlineData(BookingPaymentStatus.NotRequired, "No charge event")]
    [InlineData(BookingPaymentStatus.AwaitingPayment, "Unpaid event")]
    public async Task PaymentStatus_Filter_Matches_The_Derived_Status_For_Unpaid_And_NotRequired_Bookings(
        BookingPaymentStatus filter, string expectedEventType)
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(10);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        // No charge/deposit -> PaymentRequired = false -> NotRequired.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "No charge event", start, 0m, 0m));
        // Charge set but not yet paid (InvoiceId/DepositCollectionPostingId both null) -> AwaitingPayment.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingId, flatId, "Unpaid event", start.AddHours(5), 500m, 0m));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, null, null, filter, null, null, 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(b => b.EventType == expectedEventType);
    }

    [Fact]
    public async Task Combined_Filters_Apply_Together_Before_Pagination()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset monthStart = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset monthEnd = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        // Matches every filter.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingA, flatId, "Wedding Reception", monthStart.AddDays(5)));
        // Wrong building.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingB, flatId, "Wedding Reception", monthStart.AddDays(6)));
        // Wrong event type.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingA, flatId, "Birthday Party", monthStart.AddDays(7)));
        // Outside date range.
        db.Set<Booking>().Add(MakeBooking(tenantId, facilityId, buildingA, flatId, "Wedding Reception", monthEnd.AddDays(1)));
        await db.SaveChangesAsync();

        BookingRepository repository = new(db);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantId, null, null, null, buildingA, "wedding", null, monthStart, monthEnd, 1, 20, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle(b => b.BuildingId == buildingA && b.EventType == "Wedding Reception");
    }

    [Fact]
    public async Task Filtered_Search_Never_Returns_Bookings_From_Another_Tenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(10);

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Booking>().Add(MakeBooking(tenantA, facilityId, buildingId, flatId, "Shared Event Name", start));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Booking>().Add(MakeBooking(tenantB, facilityId, buildingId, flatId, "Shared Event Name", start));
            await dbB.SaveChangesAsync();
        }

        await using MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA);
        BookingRepository repository = new(asTenantA);
        PagedResult<Booking> result = await repository.SearchAsync(
            tenantA, null, null, null, buildingId, "shared", null, start.AddDays(-1), start.AddDays(1), 1, 20,
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.TenantId.Should().Be(tenantA);
    }
}
